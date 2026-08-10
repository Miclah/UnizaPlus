// Drag-and-drop for the schedule grid: Pointer Events only (works for mouse, touch and pen
// with one code path), no page reload, optimistic UI with server persistence, and an
// undo/redo command stack. See UnizaPlus.Web/Pages/Index.cshtml for the data this reads
// (window.scheduleData) and the markup it renders into (#scheduleGrid / #scheduleDayList).
(function () {
    "use strict";

    const data = window.scheduleData;
    if (!data || !Array.isArray(data.items)) {
        return;
    }

    const state = {
        items: data.items.map(function (item) { return Object.assign({}, item); }),
    };

    let grid = null;
    let dayListEl = null;

    // ---- Pure layout/conflict logic, ported 1:1 from the C# it mirrors so a client-side
    // re-render always agrees with what the server would have rendered:
    // ScheduleOverlapChecker.FindConflictingItemIds / IsWithinBoundaries and
    // ScheduleGridLayout.ComputeDayLayout / IndexModel.GetItemStyle. ----

    function itemsOverlap(a, b) {
        return a.startHour < b.startHour + b.duration && b.startHour < a.startHour + a.duration;
    }

    function computeConflicts(items) {
        const conflicting = new Set();
        for (let i = 0; i < items.length; i++) {
            for (let j = i + 1; j < items.length; j++) {
                const a = items[i];
                const b = items[j];
                if (a.day === b.day && itemsOverlap(a, b)) {
                    conflicting.add(a.id);
                    conflicting.add(b.id);
                }
            }
        }
        return conflicting;
    }

    function wouldConflict(itemId, day, startHour, duration) {
        return state.items.some(function (i) {
            return i.id !== itemId && i.day === day &&
                startHour < i.startHour + i.duration && i.startHour < startHour + duration;
        });
    }

    function withinBounds(startHour, duration) {
        return startHour >= data.minHour && (startHour + duration) <= (data.maxHour + 1);
    }

    function groupIntoOverlapClusters(sortedItems) {
        const clusters = [];
        sortedItems.forEach(function (item) {
            const touching = clusters.filter(function (cluster) {
                return cluster.some(function (existing) { return itemsOverlap(existing, item); });
            });

            if (touching.length === 0) {
                clusters.push([item]);
                return;
            }

            const target = touching[0];
            target.push(item);
            for (let k = 1; k < touching.length; k++) {
                const merged = touching[k];
                target.push.apply(target, merged);
                clusters.splice(clusters.indexOf(merged), 1);
            }
        });
        return clusters;
    }

    /** Map<itemId, {columnIndex, columnCount}> for one day's items (Google-Calendar-style column packing). */
    function computeDayLayout(dayItems) {
        const result = new Map();
        const items = dayItems.slice().sort(function (a, b) {
            return a.startHour - b.startHour || a.id - b.id;
        });
        if (items.length === 0) {
            return result;
        }

        groupIntoOverlapClusters(items).forEach(function (cluster) {
            const columnEndHour = [];
            const columnByItemId = new Map();

            cluster.forEach(function (item) {
                let column = columnEndHour.findIndex(function (endHour) { return endHour <= item.startHour; });
                if (column < 0) {
                    column = columnEndHour.length;
                    columnEndHour.push(0);
                }
                columnEndHour[column] = item.startHour + item.duration;
                columnByItemId.set(item.id, column);
            });

            const columnCount = columnEndHour.length;
            cluster.forEach(function (item) {
                result.set(item.id, { columnIndex: columnByItemId.get(item.id), columnCount: columnCount });
            });
        });

        return result;
    }

    function round3(n) {
        return Math.round(n * 1000) / 1000;
    }

    function getItemStyle(position, duration) {
        if (!position) {
            return "";
        }
        const widthPercent = 100 * duration / position.columnCount;
        const leftPercent = position.columnIndex * widthPercent;
        return "left: calc(" + round3(leftPercent) + "% + 2px); width: calc(" + round3(widthPercent) + "% - 4px);";
    }

    // ---- Rendering: rebuilds only the item content of the (already server-rendered) grid
    // cells and day-list sections, using textContent throughout so item data (subject,
    // professor, ...) can never be interpreted as markup. ----

    function buildScheduleItemElement(item, isConflict, position) {
        const el = document.createElement("div");
        const isNarrow = !!position && position.columnCount > 1;
        el.className = "schedule-item type-" + item.type + (isConflict ? " has-conflict" : "") + (isNarrow ? " is-narrow" : "");
        el.dataset.id = String(item.id);
        el.dataset.duration = String(item.duration);
        el.dataset.day = item.day;
        el.dataset.hour = String(item.startHour);
        el.style.cssText = getItemStyle(position, item.duration);

        const endHour = item.startHour + item.duration;
        const typeLabel = data.typeLabels[item.type] || item.type;
        let tooltip = item.subject + " (" + typeLabel + ")\n" +
            item.startHour + ":00-" + endHour + ":00, " + item.classroom + "\n" + item.professor;
        if (isConflict) {
            tooltip = data.text.conflictTooltipPrefix + "\n" + tooltip;
        }
        el.title = tooltip;

        if (isConflict) {
            const badge = document.createElement("span");
            badge.className = "item-conflict-badge";
            badge.setAttribute("aria-hidden", "true");
            badge.textContent = "⚠";
            el.appendChild(badge);
        }

        const typeBadge = document.createElement("span");
        typeBadge.className = "item-type-badge";
        typeBadge.textContent = item.type;
        el.appendChild(typeBadge);

        if (!isNarrow) {
            const time = document.createElement("div");
            time.className = "item-time";
            time.textContent = item.startHour + ":00-" + endHour + ":00";
            el.appendChild(time);
        }

        const subject = document.createElement("div");
        subject.className = "item-subject";
        subject.textContent = item.subject;
        el.appendChild(subject);

        if (!isNarrow) {
            const details = document.createElement("div");
            details.className = "item-details";
            const prof = document.createElement("span");
            prof.textContent = item.professor;
            const room = document.createElement("span");
            room.textContent = item.classroom;
            details.appendChild(prof);
            details.appendChild(room);
            el.appendChild(details);
        }

        return el;
    }

    function buildDayListItemElement(item, isConflict) {
        const el = document.createElement("div");
        el.className = "day-list-item type-" + item.type + (isConflict ? " has-conflict" : "");
        el.dataset.id = String(item.id);

        const endHour = item.startHour + item.duration;
        const time = document.createElement("div");
        time.className = "day-list-item-time";
        time.textContent = item.startHour + ":00-" + endHour + ":00";
        el.appendChild(time);

        const body = document.createElement("div");
        body.className = "day-list-item-body";

        const title = document.createElement("div");
        title.className = "day-list-item-title";
        const typeBadge = document.createElement("span");
        typeBadge.className = "item-type-badge";
        typeBadge.textContent = item.type;
        title.appendChild(typeBadge);
        const strong = document.createElement("strong");
        strong.textContent = item.subject;
        title.appendChild(strong);
        if (isConflict) {
            const badge = document.createElement("span");
            badge.className = "item-conflict-badge";
            badge.setAttribute("aria-hidden", "true");
            badge.title = data.text.conflictBadgeTitle;
            badge.textContent = "⚠";
            title.appendChild(badge);
        }
        body.appendChild(title);

        const meta = document.createElement("div");
        meta.className = "day-list-item-meta";
        meta.textContent = item.classroom + " · " + item.professor;
        body.appendChild(meta);

        el.appendChild(body);
        return el;
    }

    function renderGrid(conflicts) {
        if (!grid) {
            return;
        }
        grid.querySelectorAll(".hour-cell").forEach(function (cell) { cell.innerHTML = ""; });

        data.days.forEach(function (day) {
            const dayItems = state.items.filter(function (i) { return i.day === day; });
            const layout = computeDayLayout(dayItems);
            dayItems.forEach(function (item) {
                const cell = grid.querySelector('.hour-cell[data-day="' + day + '"][data-hour="' + item.startHour + '"]');
                if (!cell) {
                    return; // outside the currently rendered hour range
                }
                cell.appendChild(buildScheduleItemElement(item, conflicts.has(item.id), layout.get(item.id)));
            });
        });
    }

    function renderDayList(conflicts) {
        if (!dayListEl) {
            return;
        }
        data.days.forEach(function (day) {
            const container = dayListEl.querySelector('.day-list-items[data-day="' + day + '"]');
            if (!container) {
                return;
            }
            container.innerHTML = "";

            const dayItems = state.items
                .filter(function (i) { return i.day === day; })
                .sort(function (a, b) { return a.startHour - b.startHour; });

            if (dayItems.length === 0) {
                const empty = document.createElement("p");
                empty.className = "day-list-empty";
                empty.textContent = data.text.noClasses;
                container.appendChild(empty);
                return;
            }

            dayItems.forEach(function (item) {
                container.appendChild(buildDayListItemElement(item, conflicts.has(item.id)));
            });
        });
    }

    function renderConflictSummary(conflicts) {
        const wrap = document.getElementById("conflictSummary");
        const textEl = document.getElementById("conflictSummaryText");
        if (!wrap || !textEl) {
            return;
        }

        const count = conflicts.size;
        if (count === 0) {
            wrap.style.display = "none";
            textEl.textContent = "";
            return;
        }

        wrap.style.display = "inline-flex";
        const template = count === 1 ? data.text.conflictSingular : data.text.conflictPlural;
        textEl.textContent = template.replace("{0}", String(count));
    }

    function render() {
        const conflicts = computeConflicts(state.items);
        renderGrid(conflicts);
        renderDayList(conflicts);
        renderConflictSummary(conflicts);
    }

    function viewDetails(id) {
        window.location.href = "/ScheduleDetail/" + id;
    }

    // ---- Server persistence: every move (including ones replayed by undo/redo) sends only
    // {id, day, startHour} for the one item that changed - never the rest of the schedule. ----

    async function persistMove(id, day, startHour) {
        try {
            const response = await fetch("/api/schedule/move", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ id: id, day: day, startHour: startHour }),
            });
            if (response.ok) {
                return { ok: true };
            }
            const errorText = await response.text();
            return { ok: false, errorText: errorText };
        } catch (err) {
            return { ok: false, errorText: null };
        }
    }

    // ---- Undo/redo: a stack of MoveItemCommand objects (id + from/to position), not schedule
    // snapshots. Undo/redo replay the same single-item move against the server as an ordinary
    // drag would, so the server never needs to know undo/redo exist. ----

    function MoveItemCommand(itemId, from, to) {
        this.itemId = itemId;
        this.from = from;
        this.to = to;
    }

    MoveItemCommand.prototype.execute = function () { return this._apply(this.to); };
    MoveItemCommand.prototype.undo = function () { return this._apply(this.from); };

    MoveItemCommand.prototype._apply = async function (position) {
        const item = state.items.find((i) => i.id === this.itemId);
        if (!item) {
            return false;
        }

        const previous = { day: item.day, startHour: item.startHour };
        item.day = position.day;
        item.startHour = position.startHour;
        render();

        const result = await persistMove(this.itemId, position.day, position.startHour);
        if (!result.ok) {
            item.day = previous.day;
            item.startHour = previous.startHour;
            render();
            alert(result.errorText ? data.text.moveFailed.replace("{0}", result.errorText) : data.text.moveError);
            return false;
        }
        return true;
    };

    const undoStack = [];
    const redoStack = [];
    let commandInFlight = false;

    async function runCommand(cmd) {
        if (commandInFlight) {
            return;
        }
        commandInFlight = true;
        const ok = await cmd.execute();
        commandInFlight = false;
        if (ok) {
            undoStack.push(cmd);
            redoStack.length = 0;
        }
    }

    async function undo() {
        if (commandInFlight || undoStack.length === 0) {
            return;
        }
        commandInFlight = true;
        const cmd = undoStack.pop();
        const ok = await cmd.undo();
        commandInFlight = false;
        (ok ? redoStack : undoStack).push(cmd);
    }

    async function redo() {
        if (commandInFlight || redoStack.length === 0) {
            return;
        }
        commandInFlight = true;
        const cmd = redoStack.pop();
        const ok = await cmd.execute();
        commandInFlight = false;
        (ok ? undoStack : redoStack).push(cmd);
    }

    function onKeyDown(evt) {
        const tag = (evt.target && evt.target.tagName || "").toLowerCase();
        if (tag === "input" || tag === "textarea" || tag === "select" || (evt.target && evt.target.isContentEditable)) {
            return;
        }
        if (!(evt.ctrlKey || evt.metaKey) || evt.altKey) {
            return;
        }

        const key = evt.key.toLowerCase();
        if (key === "z" && !evt.shiftKey) {
            evt.preventDefault();
            undo();
        } else if (key === "y" || (key === "z" && evt.shiftKey)) {
            evt.preventDefault();
            redo();
        }
    }

    // ---- Dragging: Pointer Events only, so mouse/touch/pen share one implementation. The
    // dragged element stays put (dimmed) and a fixed-position clone follows the pointer, so
    // the grid layout never shifts mid-drag; drop target and conflict/out-of-range preview are
    // resolved with elementFromPoint under that clone. ----

    const DRAG_THRESHOLD_PX = 6;
    let dragCtx = null;

    function cellAtPoint(clientX, clientY) {
        const el = document.elementFromPoint(clientX, clientY);
        if (!el) {
            return null;
        }
        const cell = el.closest(".hour-cell");
        return (cell && grid.contains(cell)) ? cell : null;
    }

    function clearHoverCell() {
        if (dragCtx && dragCtx.lastCell) {
            dragCtx.lastCell.classList.remove("drop-hover", "drop-ok", "drop-conflict", "drop-invalid");
            dragCtx.lastCell = null;
        }
    }

    function updateHoverCell(clientX, clientY) {
        const cell = cellAtPoint(clientX, clientY);
        if (cell === dragCtx.lastCell) {
            return;
        }
        clearHoverCell();
        if (!cell) {
            return;
        }

        const day = cell.dataset.day;
        const hour = parseInt(cell.dataset.hour, 10);
        cell.classList.add("drop-hover");
        if (!withinBounds(hour, dragCtx.item.duration)) {
            cell.classList.add("drop-invalid");
        } else if (wouldConflict(dragCtx.id, day, hour, dragCtx.item.duration)) {
            cell.classList.add("drop-conflict");
        } else {
            cell.classList.add("drop-ok");
        }
        dragCtx.lastCell = cell;
    }

    function startVisualDrag() {
        const ctx = dragCtx;
        ctx.el.classList.add("dragging");

        const ghost = ctx.el.cloneNode(true);
        ghost.classList.remove("dragging");
        ghost.classList.add("schedule-item-ghost");
        ghost.style.width = ctx.width + "px";
        ghost.style.height = ctx.height + "px";
        ghost.style.left = "0px";
        ghost.style.top = "0px";
        document.body.appendChild(ghost);
        ctx.ghost = ghost;
        document.body.classList.add("schedule-dragging");
    }

    function positionGhost(clientX, clientY) {
        if (!dragCtx.ghost) {
            return;
        }
        dragCtx.ghost.style.transform = "translate(" + (clientX - dragCtx.grabDX) + "px, " + (clientY - dragCtx.grabDY) + "px)";
    }

    function endVisualDrag() {
        if (!dragCtx) {
            return;
        }
        dragCtx.el.classList.remove("dragging");
        if (dragCtx.ghost) {
            dragCtx.ghost.remove();
        }
        clearHoverCell();
        document.body.classList.remove("schedule-dragging");
    }

    function onPointerDown(evt) {
        const el = evt.target.closest(".schedule-item");
        if (!el || !grid.contains(el)) {
            return;
        }
        if (evt.pointerType === "mouse" && evt.button !== 0) {
            return;
        }

        const id = parseInt(el.dataset.id, 10);
        const item = state.items.find(function (i) { return i.id === id; });
        if (!item) {
            return;
        }

        const rect = el.getBoundingClientRect();
        dragCtx = {
            id: id,
            item: item,
            pointerId: evt.pointerId,
            el: el,
            grabDX: evt.clientX - rect.left,
            grabDY: evt.clientY - rect.top,
            width: rect.width,
            height: rect.height,
            startClientX: evt.clientX,
            startClientY: evt.clientY,
            moved: false,
            ghost: null,
            lastCell: null,
        };
        el.setPointerCapture(evt.pointerId);
        evt.preventDefault();
    }

    function onPointerMove(evt) {
        if (!dragCtx || evt.pointerId !== dragCtx.pointerId) {
            return;
        }

        if (!dragCtx.moved) {
            const dx = evt.clientX - dragCtx.startClientX;
            const dy = evt.clientY - dragCtx.startClientY;
            if (Math.hypot(dx, dy) < DRAG_THRESHOLD_PX) {
                return;
            }
            dragCtx.moved = true;
            startVisualDrag();
        }

        positionGhost(evt.clientX, evt.clientY);
        updateHoverCell(evt.clientX, evt.clientY);
    }

    function onPointerUp(evt) {
        if (!dragCtx || evt.pointerId !== dragCtx.pointerId) {
            return;
        }
        const ctx = dragCtx;
        try { ctx.el.releasePointerCapture(ctx.pointerId); } catch (err) { /* already released */ }

        if (!ctx.moved) {
            dragCtx = null;
            viewDetails(ctx.id);
            return;
        }

        const target = cellAtPoint(evt.clientX, evt.clientY);
        endVisualDrag();
        dragCtx = null;

        if (!target) {
            return;
        }

        const newDay = target.dataset.day;
        const newHour = parseInt(target.dataset.hour, 10);
        if (newDay === ctx.item.day && newHour === ctx.item.startHour) {
            return; // dropped back where it started
        }
        if (!withinBounds(newHour, ctx.item.duration)) {
            return; // out of the supported hour range - silently snap back, no doomed API call
        }

        runCommand(new MoveItemCommand(ctx.id, { day: ctx.item.day, startHour: ctx.item.startHour }, { day: newDay, startHour: newHour }));
    }

    function onPointerCancel(evt) {
        if (!dragCtx || evt.pointerId !== dragCtx.pointerId) {
            return;
        }
        endVisualDrag();
        dragCtx = null;
    }

    function init() {
        grid = document.getElementById("scheduleGrid");
        dayListEl = document.getElementById("scheduleDayList");
        if (!grid) {
            return; // empty-schedule state: nothing to drag, nothing to undo
        }

        grid.addEventListener("pointerdown", onPointerDown);
        document.addEventListener("pointermove", onPointerMove);
        document.addEventListener("pointerup", onPointerUp);
        document.addEventListener("pointercancel", onPointerCancel);

        if (dayListEl) {
            dayListEl.addEventListener("click", function (evt) {
                const el = evt.target.closest(".day-list-item");
                if (el) {
                    viewDetails(parseInt(el.dataset.id, 10));
                }
            });
        }

        document.addEventListener("keydown", onKeyDown);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();

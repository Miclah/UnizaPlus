# UnizaPlus

UnizaPlus is a comprehensive web application developed as a university project to improve the schedule management experience for students at the University of Žilina (UNIZA). It introduces advanced features and a modern interface that go beyond the capabilities of the official university scheduling system.

---

## Project Overview

The official UNIZA schedule system is primarily a static, read-only platform that provides limited functionality for students and faculty. UnizaPlus was created to transform this rigid system into a dynamic and customizable schedule management tool.

By extracting schedule data directly from the official university portal, UnizaPlus offers a significantly enhanced interface, allowing users to view, modify, and manage their schedules with a level of flexibility not available in the existing system.

---

## Key Features

### Enhanced Schedule Management
- **Add New Classes**  
  Users can create and insert custom classes that do not exist in the official schedule.
- **Edit Existing Classes**  
  Modify any detail of scheduled items, including professor, classroom, and timing.
- **Drag-and-Drop Rescheduling**  
  Visually move classes between different time slots with a user-friendly drag-and-drop interface—a feature completely missing in the university system.
- **Conflict Detection**  
  Automatic validation to prevent scheduling conflicts and overlapping classes.

### Data Extraction & Processing
- **Automated Schedule Retrieval**  
  Uses Selenium WebDriver to automatically extract full schedule information from the university portal.
- **Enhanced Data Collection**  
  Gathers detailed class data by navigating through multiple linked pages within the portal.
- **Intelligent Caching**  
  Implements caching mechanisms for professor, classroom, and subject information to improve performance and reduce redundant data fetching.

### Improved Visualization
- **Intuitive Schedule Display**  
  Presents a weekly view with color-coded classes based on type:  
  - Lectures (P) - Blue  
  - Laboratory Exercises (L) - Green  
  - Exercises (C) - Yellow
- **Comprehensive Class Details**  
  Displays extensive class information not easily accessible in the official system.

### Offline Functionality
- **Local Storage**  
  Maintains schedule data locally in CSV format, allowing offline access.
- **Persistence**  
  Saves user-made changes so schedules remain accessible and customizable without an internet connection.
- **Manual and Automatic Refresh**  
  Users can refresh schedule data on demand or set it to update automatically.

---

## Technical Implementation

### Architecture
- **UnizaPlus.Web** — ASP.NET Core Razor Pages web application serving as the user interface.  
- **UnizaPlusBackEnd** — Console-based scraper application responsible for data extraction.  
- The project follows a service-oriented design pattern with clear separation between data acquisition, storage, and presentation layers.

### Technologies Used
- **.NET 8** — Utilizes the latest features of C# and the .NET ecosystem.  
- **Selenium WebDriver** — Automates browser interaction for reliable data scraping.  
- **Bootstrap** — Ensures responsive and modern frontend design.  
- **JavaScript** — Enables dynamic UI behaviors such as drag-and-drop scheduling.  
- **CSV File System** — Provides lightweight and portable data persistence.

---

## Advantages Compared to the Official University System

| Feature               | UnizaPlus                          | University System           |
|-----------------------|----------------------------------|----------------------------|
| Class Creation        | ✅ Add custom classes             | ❌ Read-only                |
| Schedule Editing      | ✅ Full modification capabilities| ❌ No editing allowed       |
| Visual Manipulation   | ✅ Drag-and-drop interface        | ❌ Static, non-interactive  |
| Offline Access        | ✅ Works without internet         | ❌ Requires constant connection |
| Detailed Information  | ✅ Comprehensive class details    | ✅ Basic class information  |
| User Experience       | ✅ Modern, intuitive interface    | ❌ Basic legacy interface   |

---

## Summary

UnizaPlus significantly enhances the university's scheduling system by addressing its key limitations—most notably the lack of editing capabilities and the inability to rearrange schedule items. By transforming a static viewer into a fully interactive and customizable schedule manager, UnizaPlus provides both students and faculty with greater control, flexibility, and convenience in managing their academic timetables.

---

## Getting Started

### Prerequisites
- .NET 8.0 SDK  
- Google Chrome browser (required for Selenium)  
- Internet connection for initial data scraping

### Installation and Usage
1. Clone the project repository.  
2. Build the solution using your preferred .NET development environment.  
3. Configure university credentials for schedule scraping (if needed).  
4. Run the web application to access the enhanced schedule interface.

---

## Future Work

Potential extensions to this project include:  
- Integration with the official university calendar system  
- Push notifications for schedule changes  
- Support for personal notes and reminders  
- Inclusion of exam schedules  
- Development of a companion mobile app  
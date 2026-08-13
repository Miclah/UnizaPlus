namespace UnizaPlus.Web
{
    /// <summary>
    /// Marker type for the shared IStringLocalizer&lt;SharedResource&gt; - see Resources/SharedResource.*.resx.
    /// Must live in the project's root namespace (not UnizaPlus.Web.Resources): with
    /// ResourcesPath = "Resources", the localizer factory combines the root namespace with
    /// ResourcesPath and the type's own namespace, so a type namespaced under .Resources would
    /// double up and look for Resources/Resources/SharedResource.resx instead.
    /// </summary>
    public class SharedResource
    {
    }
}

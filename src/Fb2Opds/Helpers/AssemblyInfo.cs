namespace Vurdalakov
{
    using System;
    using System.Reflection;

    internal sealed class AssemblyInfo
    {
        public String FileName { get; }

        public String Product { get; }

        public Version Version { get; }

        public String Company { get; }

        public String Copyright { get; }

        public String Description { get; }

        private AssemblyInfo(Assembly assembly)
        {
            var assemblyName = assembly.GetName();

            // Assembly.Location is empty in single-file apps, so the name comes from the assembly identity
            this.FileName = assemblyName.Name;
            this.Version = assemblyName.Version;

            this.Product = NullIfEmpty(assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product) ?? this.FileName;
            this.Company = NullIfEmpty(assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company);
            this.Copyright = NullIfEmpty(assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright);
            this.Description = NullIfEmpty(assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description);

            String NullIfEmpty(String value) => String.IsNullOrWhiteSpace(value) ? null : value;
        }

        public static AssemblyInfo Read() => AssemblyInfo.Read(Assembly.GetExecutingAssembly());

        public static AssemblyInfo Read(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            return new AssemblyInfo(assembly);
        }
    }
}
namespace Vurdalakov
{
    using System;
    using System.IO;
    using System.Reflection;

    internal class AssemblyInfo
    {
        public String FileName { get; private set; }

        public String Product { get; private set; }

        public Version Version { get; private set; }

        public String Company { get; private set; }

        public String Copyright { get; private set; }

        public String Description { get; private set; }

        public static AssemblyInfo Read() => AssemblyInfo.Read(Assembly.GetExecutingAssembly());

        public static AssemblyInfo Read(Assembly assembly)
        {
            var assemblyPath = assembly.Location;
            var assemblyFileName = Path.GetFileNameWithoutExtension(assemblyPath);
            var assemblyName = assembly.GetName();

            return new AssemblyInfo
            {
                FileName = assemblyFileName,
                Product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product,
                Version = assemblyName?.Version,
                Company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company,
                Copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright,
                Description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description,
            };
        }
    }
}
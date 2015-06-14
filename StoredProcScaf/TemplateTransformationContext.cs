using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using StoredProcScaf.UI;

namespace StoredProcScaf
{
    public class TemplateTransformationContext
    {
        public string ClassName { get; private set; }
        public ClassAttribute ClassAttribute { get; set; }
        public IEnumerable<Property> Properties { get; private set; }
        public string ConstructorParameters { get; set; }

        public IEnumerable<string> ConstructorParameterAssigments { get; set; }

        public static TemplateTransformationContext Create(ObjectInfo objectInfo, IEnumerable<ParameterInfo> parameterInfos)
        {
            var parameters = parameterInfos as ParameterInfo[] ?? parameterInfos.ToArray();

            var properties = CreateProperties(parameters);
            var constructorParameters = CreateConstructorParameters(parameters).Aggregate((b, n) => string.Format("{0}, {1}", b, n));
            var constructorParameterAssigments = CreateConstructorParameterAssigments(parameters);
            
            var tc = new TemplateTransformationContext
            {
                ClassAttribute = new ClassAttribute(objectInfo),
                ClassName = string.Format("{0}StoredProc", objectInfo.Name.Replace(" ",string.Empty)),//TODO add suffix to the config file
                Properties = properties,
                ConstructorParameters = constructorParameters,
                ConstructorParameterAssigments = constructorParameterAssigments
            };

            return tc;
        }

        private static IEnumerable<string> CreateConstructorParameterAssigments(IEnumerable<ParameterInfo> parameterInfos)
        {
            return parameterInfos.Select(parameterInfo => string.Format("{0} = {1};", parameterInfo.Name, ToCamelCase(parameterInfo.Name)));
        }

        private static IEnumerable<string> CreateConstructorParameters(IEnumerable<ParameterInfo> parameterInfos)
        {
            return parameterInfos.Select(parameterInfo => string.Format("{0} {1}", MappingReader.Sql2ClrMap.ContainsKey(parameterInfo.Type)? MappingReader.Sql2ClrMap[parameterInfo.Type]:parameterInfo.Type, ToCamelCase(parameterInfo.Name)));
        }

        private static string ToCamelCase(string name)
        {
            return name.Replace(name.First(), char.ToLower(name.First()));
        }

        private static IEnumerable<Property> CreateProperties(IEnumerable<ParameterInfo> parameterInfos)
        {
            var handledParameters = HandleDataTableTypes(parameterInfos);
            foreach (var parameterInfo in handledParameters)
            {
                yield return new Property
                {
                    Name = parameterInfo.Name,
                    CSharpType = GetCSharpType(parameterInfo),
                    Attribute = new PropertyAttribute
                    {
                        DbType = GetDbType(parameterInfo),
                        Length = parameterInfo.Length.ToString(CultureInfo.InvariantCulture),
                        Name = parameterInfo.Name
                    }
                };
            }
        }

        private static string GetDbType(ParameterInfo parameterInfo)
        {
            // if not in mappings we assume that parameter is user defined table type.
            return MappingReader.Sql2DbTypeMap.ContainsKey(parameterInfo.Type) ? MappingReader.Sql2DbTypeMap[parameterInfo.Type] : "SqlDbType.Udt";
        }

        private static string GetCSharpType(ParameterInfo parameterInfo)
        {
            // if not in mappings we assume that parameter is user defined table type.
            return MappingReader.Sql2ClrMap.ContainsKey(parameterInfo.Type) ? MappingReader.Sql2ClrMap[parameterInfo.Type] : parameterInfo.Type;
        }

        // ReSharper disable PossibleMultipleEnumeration
        private static IEnumerable<ParameterInfo> HandleDataTableTypes(IEnumerable<ParameterInfo> parameterInfos)
        {
           
            var dbTableTypeParameters = parameterInfos.Where(pi => !MappingReader.Sql2DbTypeMap.ContainsKey(pi.Type));
            if (dbTableTypeParameters.Any())
            {
                var wnd = new ParameterTypeWindow();
                var myViewModel = new MyViewModel();
                var parameters = dbTableTypeParameters.Select(ttp => new Parameter(ttp.Type, ttp.Name));
                myViewModel.MyParameters = new MyParameters(parameters);
                wnd.DataContext = myViewModel;
                wnd.ShowDialog();

                Map(dbTableTypeParameters, pi =>
                {
                    var mapped = myViewModel.MyParameters.FirstOrDefault(tp => tp.MyParameter.Equals(pi.Name));
                    if (mapped != null)
                    {
                        pi.Type = mapped.MyType;
                    }
                });
                
            }

            return parameterInfos;
            
        }
        // ReSharper restore PossibleMultipleEnumeration

        private static void Map<T>(IEnumerable<T> sequence, Action<T> mapAction)
        {
            foreach (T t in sequence)
            {
                mapAction(t);
            }
        }
    }

   

    public class ClassAttribute
    {
        public ObjectInfo ObjectInfo { get; set; }

        public ClassAttribute(ObjectInfo objectInfo)
        {
            ObjectInfo = objectInfo;
        }

        public override string ToString()
        {
            return string.Format("[StoredProcedure(\"{0}.{1}\")]", ObjectInfo.Owner, ObjectInfo.Name);
        }
    }

    public class Property
    {
        public string Name { get; set; }
        public PropertyAttribute Attribute { get; set; }
        public string CSharpType { get; set; }
    }

    public class PropertyAttribute
    {
        public string Name { get; set; }
        public string DbType { get; set; }
        public string Length { get; set; }

        public override string ToString()
        {
            return string.Format("[StoredProcedureParameter({0})]", DbType);
        }
    }
}
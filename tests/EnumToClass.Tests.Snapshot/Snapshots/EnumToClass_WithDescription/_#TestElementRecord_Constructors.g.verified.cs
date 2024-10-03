﻿//HintName: TestElementRecord_Constructors.g.cs
//<auto-generated />                        
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

#nullable enable
namespace VRT.Generators.Tests
{
    public partial record TestElementRecord            
    {                    
        private TestElementRecord(VRT.Generators.Tests.TestElements value, string description)
        {
            Name = value.ToString();
            IsEmpty = value == default(VRT.Generators.Tests.TestElements);
            Value = value;
            Description = description;
        }
        public static TestElementRecord Empty { get; } = new TestElementRecord(VRT.Generators.Tests.TestElements.None,"Empty element");
        public string Name { get; }
        public VRT.Generators.Tests.TestElements Value { get; }
        public bool IsEmpty { get; }                    

        public string Description { get; }                                        
        

        public override string ToString() => Name;    
        
        public static IReadOnlyCollection<TestElementRecord> GetAll() => ValueByNameMap.Values;
            
        public static IEnumerable<TestElementRecord> GetByName(IEnumerable<string> names)
            => names.Select(GetByName).Where(p => p.IsEmpty == false);

        public static TestElementRecord GetByName(string name)
        {
            return ValueByNameMap.TryGetValue(name, out var value)
                ? value
                : Empty;
        }
        public static implicit operator TestElementRecord(string name) => GetByName(name);
        public static implicit operator string(TestElementRecord value) => value.Name;
        public static implicit operator VRT.Generators.Tests.TestElements(TestElementRecord value) => value.Value;
        public static implicit operator TestElementRecord(VRT.Generators.Tests.TestElements value) => GetByName(value.ToString());
        public static implicit operator int(TestElementRecord value) => (int) value.Value;
    }
}
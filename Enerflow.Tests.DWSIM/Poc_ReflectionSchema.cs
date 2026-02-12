
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using DWSIM.UnitOperations.UnitOperations;
using DWSIM.Interfaces;

namespace Enerflow.Tests.DWSIM.Poc
{
    public class ReflectionSchemaTests
    {
        private readonly ITestOutputHelper _output;

        public ReflectionSchemaTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // 1. The Schema Generator (Server-Side)
        // This runs ONCE at startup or build time to generate the JSON contract
        private class SchemaGenerator
        {
            public static ComponentSchema GenerateSchema<T>()
            {
                var type = typeof(T);
                var schema = new ComponentSchema
                {
                    Type = type.Name,
                    Properties = new List<PropertySchema>()
                };

                // DWSIM specific: We only want properties that are editable parameters
                // In a real implementation, we might filter by Attribute or naming convention
                // For this PoC, we look for simple types that are read/write
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite && p.CanRead)
                    .Where(p => IsSimpleType(p.PropertyType));

                foreach (var prop in properties)
                {
                    schema.Properties.Add(new PropertySchema
                    {
                        Name = prop.Name,
                        DataType = prop.PropertyType.Name,
                        // We can get descriptions if they have attributes, 
                        // or default values by instantiating a dummy object
                    });
                }

                return schema;
            }

            private static bool IsSimpleType(Type type)
            {
                var t = Nullable.GetUnderlyingType(type) ?? type;
                return t.IsPrimitive || t == typeof(string) || t.IsEnum || t == typeof(decimal);
            }
        }

        // 2. The Contract (Shared/JSON)
        // This is what the Frontend receives to build the UI dynamically
        private class ComponentSchema
        {
            public string Type { get; set; }
            public List<PropertySchema> Properties { get; set; }
        }

        private class PropertySchema
        {
            public string Name { get; set; }
            public string DataType { get; set; } // "Double", "String", "Int32", "CalculationMode"
            // UI hints could go here: Min, Max, Units, Options (for enums)
        }

        [Fact]
        public void Can_Generate_Schema_For_Heater()
        {
            // Act: Generate schema for DWSIM Heater
            var schema = SchemaGenerator.GenerateSchema<Heater>();

            // Assert: Verify we got the properties we care about
            Assert.Equal("Heater", schema.Type);
            
            _output.WriteLine($"Schema for {schema.Type}:");
            foreach (var prop in schema.Properties)
            {
                _output.WriteLine($" - {prop.Name} ({prop.DataType})");
            }

            // Key properties we expect DWSIM's Heater to have
            Assert.Contains(schema.Properties, p => p.Name == "Efficiency"); 
            Assert.Contains(schema.Properties, p => p.Name == "PressureDrop"); // Or DeltaP, depending on DWSIM naming
            Assert.Contains(schema.Properties, p => p.Name == "HeatDuty"); // Or DeltaQ
        }

        [Fact]
        public void Can_Configure_DWSIM_Object_From_Dictionary()
        {
            // Arrange: The "Client" sends a JSON payload (simulated as Dictionary)
            var clientPayload = new Dictionary<string, object>
            {
                { "Efficiency", 0.85 },      // 85%
                { "PressureDrop", 5000.0 },  // 5 kPa (assuming Pa)
                { "TemperatureChange", 10.0 } // 10 K
            };

            // Act: The Worker instantiates and configures via Reflection
            var heater = new Heater(); // DWSIM Object
            var type = heater.GetType();

            foreach (var kvp in clientPayload)
            {
                var propInfo = type.GetProperty(kvp.Key);
                if (propInfo != null)
                {
                    // Handle type conversion (JSON numbers are usually doubles or decimals)
                    var targetType = Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType;
                    var value = Convert.ChangeType(kvp.Value, targetType);
                    
                    propInfo.SetValue(heater, value);
                }
            }

            // Assert: The DWSIM object actually updated
            Assert.Equal(0.85, heater.Efficiency);
            Assert.Equal(5000.0, heater.PressureDrop);
            Assert.Equal(10.0, heater.TemperatureChange);
            
            _output.WriteLine("Heater configured successfully from dynamic dictionary!");
            _output.WriteLine($"Efficiency: {heater.Efficiency}");
            _output.WriteLine($"PressureDrop: {heater.PressureDrop}");
        }
    }
}

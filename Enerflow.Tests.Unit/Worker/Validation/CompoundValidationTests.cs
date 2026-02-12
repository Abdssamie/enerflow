using Enerflow.Domain.Entities;
using Enerflow.Domain.Entities.Streams;
using Enerflow.Domain.Entities.UnitOperations;
using Enerflow.Domain.Enums;
using Enerflow.Domain.ValueObjects;
using Enerflow.Simulation.Validation;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;

namespace Enerflow.Tests.Unit.Worker.Validation;

/// <summary>
/// Tests for compound validation (no compounds, undefined references, ShortcutColumn keys).
/// Validates Phase 2 of FlowsheetValidator.
/// </summary>
public sealed class CompoundValidationTests
{
   private readonly FlowsheetValidator _validator;

   public CompoundValidationTests()
   {
      _validator = new FlowsheetValidator(NullLogger<FlowsheetValidator>.Instance);
   }

   #region No Compounds Defined Tests

   [Fact]
   public void Validate_SimulationWithNoCompounds_ReturnsNoCompoundsDefinedError()
   {
      // Arrange
      var simulation = CreateSimulationWithoutCompounds();

      // Act
      var result = _validator.Validate(simulation, null!);

      // Assert
      result.IsValid.Should().BeFalse();
      result.Errors.Should().ContainSingle();
      result.Errors[0].Code.Should().Be(ValidationErrorCodes.NoCompoundsDefined);
      result.Errors[0].EntityType.Should().Be("Simulation");
      result.Errors[0].Message.Should().Contain("at least one compound");
   }

   [Fact]
   public void Validate_SimulationWithCompounds_PassesCompoundExistenceCheck()
   {
      // Arrange
      var simulation = CreateSimulationWithCompounds();

      // Act
      var result = _validator.Validate(simulation, null!);

      // Assert
      result.IsValid.Should().BeTrue();
      result.Errors.Should().NotContain(e => e.Code == ValidationErrorCodes.NoCompoundsDefined);
   }

   #endregion

   #region Undefined Compound Reference Tests

   [Fact]
   public void Validate_StreamReferencesUndefinedCompound_ReturnsUndefinedCompoundReferenceError()
   {
      // Arrange
      var simulation = CreateSimulationWithUndefinedCompoundReference();

      // Act
      var result = _validator.Validate(simulation, null!);

      // Assert
      result.IsValid.Should().BeFalse();
      result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.UndefinedCompoundReference);
      var error = result.Errors.First(e => e.Code == ValidationErrorCodes.UndefinedCompoundReference);
      error.EntityType.Should().Be("MaterialStream");
      error.Message.Should().Contain("Methanol");
      error.Message.Should().Contain("undefined compound");
   }

   [Fact]
   public void Validate_StreamReferencesDefinedCompounds_PassesValidation()
   {
      // Arrange
      var simulation = CreateSimulationWithValidCompoundReferences();

      // Act
      var result = _validator.Validate(simulation, null!);

      // Assert
      result.IsValid.Should().BeTrue();
      result.Errors.Should().NotContain(e => e.Code == ValidationErrorCodes.UndefinedCompoundReference);
   }

   [Fact]
   public void Validate_CompoundReferenceCaseInsensitive_PassesValidation()
   {
      // Arrange - Compound defined as "Water", referenced as "water"
      var simulation = CreateSimulationWithCaseInsensitiveCompoundReference();

      // Act
      var result = _validator.Validate(simulation, null!);

      // Assert
      result.IsValid.Should().BeTrue();
      result.Errors.Should().NotContain(e => e.Code == ValidationErrorCodes.UndefinedCompoundReference);
   }

   #endregion

   #region ShortcutColumn LightKey/HeavyKey Tests

   [Fact]
   public void Validate_ShortcutColumnWithInvalidLightKey_ReturnsInvalidLightKeyReferenceError()
   {
      // Arrange
      var simulation = CreateSimulationWithInvalidLightKey();

      // Act
      var result = _validator.Validate(simulation, null!);

      // Assert
      result.IsValid.Should().BeFalse();
      result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.InvalidLightKeyReference);
      var error = result.Errors.First(e => e.Code == ValidationErrorCodes.InvalidLightKeyReference);
      error.EntityType.Should().Be("ShortcutColumn");
      error.Message.Should().Contain("LightKey");
      error.Message.Should().Contain("invalid");
   }

   [Fact]
   public void Validate_ShortcutColumnWithInvalidHeavyKey_ReturnsInvalidHeavyKeyReferenceError()
   {
      // Arrange
      var simulation = CreateSimulationWithInvalidHeavyKey();

      // Act
      var result = _validator.Validate(simulation, null!);

      // Assert
      result.IsValid.Should().BeFalse();
      result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.InvalidHeavyKeyReference);
      var error = result.Errors.First(e => e.Code == ValidationErrorCodes.InvalidHeavyKeyReference);
      error.EntityType.Should().Be("ShortcutColumn");
      error.Message.Should().Contain("HeavyKey");
      error.Message.Should().Contain("invalid");
   }

   [Fact]
   public void Validate_ShortcutColumnWithValidKeys_PassesValidation()
   {
      // Arrange
      var simulation = CreateSimulationWithValidShortcutColumnKeys();

      // Act
      var result = _validator.Validate(simulation, null!);

      // Assert
      result.IsValid.Should().BeTrue();
      result.Errors.Should().NotContain(e => e.Code == ValidationErrorCodes.InvalidLightKeyReference);
      result.Errors.Should().NotContain(e => e.Code == ValidationErrorCodes.InvalidHeavyKeyReference);
   }

   [Fact]
   public void Validate_ShortcutColumnWithBothInvalidKeys_ReturnsMultipleErrors()
   {
      // Arrange
      var simulation = CreateSimulationWithBothInvalidKeys();

      // Act
      var result = _validator.Validate(simulation, null!);

      // Assert
      result.IsValid.Should().BeFalse();
      result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.InvalidLightKeyReference);
      result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.InvalidHeavyKeyReference);
   }

   #endregion

   #region Multiple Compound Errors Tests

   [Fact]
   public void Validate_MultipleStreamsWithUndefinedCompounds_ReturnsMultipleErrors()
   {
      // Arrange
      var simulation = CreateSimulationWithMultipleUndefinedCompounds();

      // Act
      var result = _validator.Validate(simulation, null!);

      // Assert
      result.IsValid.Should().BeFalse();
      result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
      result.Errors.Where(e => e.Code == ValidationErrorCodes.UndefinedCompoundReference)
          .Should().HaveCountGreaterThanOrEqualTo(2);
   }

   #endregion

   #region Helper Methods

   private SimulationEntity CreateSimulationWithoutCompounds()
   {
      var simulation = new SimulationEntity
      {
         Id = Guid.NewGuid(),
         Name = "Test Simulation",
         PropertyPackage = PropertyPackageType.PengRobinson,
         FlashAlgorithm = FlashAlgorithm.NestedLoops,
         SystemOfUnits = SystemOfUnits.SI,
         Status = SimulationStatus.Created
      };

      // No compounds added

      return simulation;
   }

   private SimulationEntity CreateSimulationWithCompounds()
   {
      var simulation = new SimulationEntity
      {
         Id = Guid.NewGuid(),
         Name = "Test Simulation",
         PropertyPackage = PropertyPackageType.PengRobinson,
         FlashAlgorithm = FlashAlgorithm.NestedLoops,
         SystemOfUnits = SystemOfUnits.SI,
         Status = SimulationStatus.Created
      };

      simulation.Compounds.Add(new Compound
      {
         Id = Guid.NewGuid(),
         Name = "Water",
         SimulationId = simulation.Id
      });

      simulation.Compounds.Add(new Compound
      {
         Id = Guid.NewGuid(),
         Name = "Ethanol",
         SimulationId = simulation.Id
      });

      return simulation;
   }

   private SimulationEntity CreateSimulationWithUndefinedCompoundReference()
   {
      var simulation = new SimulationEntity
      {
         Id = Guid.NewGuid(),
         Name = "Test Simulation",
         PropertyPackage = PropertyPackageType.PengRobinson,
         FlashAlgorithm = FlashAlgorithm.NestedLoops,
         SystemOfUnits = SystemOfUnits.SI,
         Status = SimulationStatus.Created
      };

      // Define only Water and Ethanol
      simulation.Compounds.Add(new Compound
      {
         Id = Guid.NewGuid(),
         Name = "Water",
         SimulationId = simulation.Id
      });

      simulation.Compounds.Add(new Compound
      {
         Id = Guid.NewGuid(),
         Name = "Ethanol",
         SimulationId = simulation.Id
      });

      // Create stream that references undefined compound "Methanol"
      var streamId = Guid.NewGuid();
      var stream = new MaterialStream
      {
         Id = streamId,
         Name = "TestStream",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(1.0),
         Composition = new Dictionary<string, double>
            {
                { "Water", 0.5 },
                { "Methanol", 0.5 } // Undefined!
            }
      };

      simulation.MaterialStreams.Add(stream);

      // Connect to unit to avoid topology errors
      simulation.UnitOperations.Add(new MixerObject
      {
         Id = Guid.NewGuid(),
         Name = "Mixer1",
         SimulationId = simulation.Id,
         InputStreamIds = new List<Guid> { streamId },
         OutputStreamIds = new List<Guid>()
      });

      return simulation;
   }

   private SimulationEntity CreateSimulationWithValidCompoundReferences()
   {
      var simulation = new SimulationEntity
      {
         Id = Guid.NewGuid(),
         Name = "Test Simulation",
         PropertyPackage = PropertyPackageType.PengRobinson,
         FlashAlgorithm = FlashAlgorithm.NestedLoops,
         SystemOfUnits = SystemOfUnits.SI,
         Status = SimulationStatus.Created
      };

      simulation.Compounds.Add(new Compound
      {
         Id = Guid.NewGuid(),
         Name = "Water",
         SimulationId = simulation.Id
      });

      simulation.Compounds.Add(new Compound
      {
         Id = Guid.NewGuid(),
         Name = "Ethanol",
         SimulationId = simulation.Id
      });

      var streamId = Guid.NewGuid();
      var stream = new MaterialStream
      {
         Id = streamId,
         Name = "TestStream",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(1.0),
         Composition = new Dictionary<string, double>
            {
                { "Water", 0.6 },
                { "Ethanol", 0.4 }
            }
      };

      simulation.MaterialStreams.Add(stream);

      var outputStreamId = Guid.NewGuid();
      simulation.MaterialStreams.Add(new MaterialStream
      {
         Id = outputStreamId,
         Name = "OutputStream",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(1.0),
         Composition = new Dictionary<string, double>
            {
                { "Water", 0.6 },
                { "Ethanol", 0.4 }
            }
      });

      simulation.UnitOperations.Add(new HeaterObject
      {
         Id = Guid.NewGuid(),
         Name = "Heater1",
         SimulationId = simulation.Id,
         InputStreamIds = new List<Guid> { streamId },
         OutputStreamIds = new List<Guid> { outputStreamId }
      });

      return simulation;
   }

   private SimulationEntity CreateSimulationWithCaseInsensitiveCompoundReference()
   {
      var simulation = new SimulationEntity
      {
         Id = Guid.NewGuid(),
         Name = "Test Simulation",
         PropertyPackage = PropertyPackageType.PengRobinson,
         FlashAlgorithm = FlashAlgorithm.NestedLoops,
         SystemOfUnits = SystemOfUnits.SI,
         Status = SimulationStatus.Created
      };

      // Define compound as "Water" (capital W)
      simulation.Compounds.Add(new Compound
      {
         Id = Guid.NewGuid(),
         Name = "Water",
         SimulationId = simulation.Id
      });

      var streamId = Guid.NewGuid();
      var stream = new MaterialStream
      {
         Id = streamId,
         Name = "TestStream",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(1.0),
         Composition = new Dictionary<string, double>
            {
                { "water", 1.0 } // lowercase - should still match
            }
      };

      simulation.MaterialStreams.Add(stream);

      var outputStreamId = Guid.NewGuid();
      simulation.MaterialStreams.Add(new MaterialStream
      {
         Id = outputStreamId,
         Name = "OutputStream",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(1.0),
         Composition = new Dictionary<string, double>
            {
                { "water", 1.0 }
            }
      });

      simulation.UnitOperations.Add(new HeaterObject
      {
         Id = Guid.NewGuid(),
         Name = "Heater1",
         SimulationId = simulation.Id,
         InputStreamIds = new List<Guid> { streamId },
         OutputStreamIds = new List<Guid> { outputStreamId }
      });

      return simulation;
   }

   private SimulationEntity CreateSimulationWithInvalidLightKey()
   {
      var simulation = new SimulationEntity
      {
         Id = Guid.NewGuid(),
         Name = "Test Simulation",
         PropertyPackage = PropertyPackageType.PengRobinson,
         FlashAlgorithm = FlashAlgorithm.NestedLoops,
         SystemOfUnits = SystemOfUnits.SI,
         Status = SimulationStatus.Created
      };

      var waterCompoundId = Guid.NewGuid();
      var ethanolCompoundId = Guid.NewGuid();

      simulation.Compounds.Add(new Compound
      {
         Id = waterCompoundId,
         Name = "Water",
         SimulationId = simulation.Id
      });

      simulation.Compounds.Add(new Compound
      {
         Id = ethanolCompoundId,
         Name = "Ethanol",
         SimulationId = simulation.Id
      });

      var inputStreamId = Guid.NewGuid();
      var outputStream1Id = Guid.NewGuid();
      var outputStream2Id = Guid.NewGuid();

      simulation.MaterialStreams.Add(new MaterialStream
      {
         Id = inputStreamId,
         Name = "Feed",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(1.0),
         Composition = new Dictionary<string, double> { { "Water", 0.5 }, { "Ethanol", 0.5 } }
      });

      simulation.MaterialStreams.Add(new MaterialStream
      {
         Id = outputStream1Id,
         Name = "Distillate",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(0.5),
         Composition = new Dictionary<string, double> { { "Ethanol", 1.0 } }
      });

      simulation.MaterialStreams.Add(new MaterialStream
      {
         Id = outputStream2Id,
         Name = "Bottoms",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(0.5),
         Composition = new Dictionary<string, double> { { "Water", 1.0 } }
      });

      // ShortcutColumn with invalid LightKey (random GUID not in compounds)
      simulation.UnitOperations.Add(new ShortcutColumnObject
      {
         Id = Guid.NewGuid(),
         Name = "Column1",
         SimulationId = simulation.Id,
         InputStreamIds = new List<Guid> { inputStreamId },
         OutputStreamIds = new List<Guid> { outputStream1Id, outputStream2Id },
         LightKey = Guid.NewGuid(), // Invalid!
         HeavyKey = waterCompoundId, // Valid
         RefluxRatio = 2.0,
         Stages = 10,
         CondenserPressure = 101325,
         ReboilerPressure = 101325
      });

      return simulation;
   }

   private SimulationEntity CreateSimulationWithInvalidHeavyKey()
   {
      var simulation = new SimulationEntity
      {
         Id = Guid.NewGuid(),
         Name = "Test Simulation",
         PropertyPackage = PropertyPackageType.PengRobinson,
         FlashAlgorithm = FlashAlgorithm.NestedLoops,
         SystemOfUnits = SystemOfUnits.SI,
         Status = SimulationStatus.Created
      };

      var waterCompoundId = Guid.NewGuid();
      var ethanolCompoundId = Guid.NewGuid();

      simulation.Compounds.Add(new Compound
      {
         Id = waterCompoundId,
         Name = "Water",
         SimulationId = simulation.Id
      });

      simulation.Compounds.Add(new Compound
      {
         Id = ethanolCompoundId,
         Name = "Ethanol",
         SimulationId = simulation.Id
      });

      var inputStreamId = Guid.NewGuid();
      var outputStream1Id = Guid.NewGuid();
      var outputStream2Id = Guid.NewGuid();

      simulation.MaterialStreams.Add(new MaterialStream
      {
         Id = inputStreamId,
         Name = "Feed",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(1.0),
         Composition = new Dictionary<string, double> { { "Water", 0.5 }, { "Ethanol", 0.5 } }
      });

      simulation.MaterialStreams.Add(new MaterialStream
      {
         Id = outputStream1Id,
         Name = "Distillate",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(0.5),
         Composition = new Dictionary<string, double> { { "Ethanol", 1.0 } }
      });

      simulation.MaterialStreams.Add(new MaterialStream
      {
         Id = outputStream2Id,
         Name = "Bottoms",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(0.5),
         Composition = new Dictionary<string, double> { { "Water", 1.0 } }
      });

      // ShortcutColumn with invalid HeavyKey
      simulation.UnitOperations.Add(new ShortcutColumnObject
      {
         Id = Guid.NewGuid(),
         Name = "Column1",
         SimulationId = simulation.Id,
         InputStreamIds = new List<Guid> { inputStreamId },
         OutputStreamIds = new List<Guid> { outputStream1Id, outputStream2Id },
         LightKey = ethanolCompoundId, // Valid
         HeavyKey = Guid.NewGuid(), // Invalid!
         RefluxRatio = 2.0,
         Stages = 10,
         CondenserPressure = 101325,
         ReboilerPressure = 101325
      });

      return simulation;
   }

   private SimulationEntity CreateSimulationWithValidShortcutColumnKeys()
   {
      var simulation = new SimulationEntity
      {
         Id = Guid.NewGuid(),
         Name = "Test Simulation",
         PropertyPackage = PropertyPackageType.PengRobinson,
         FlashAlgorithm = FlashAlgorithm.NestedLoops,
         SystemOfUnits = SystemOfUnits.SI,
         Status = SimulationStatus.Created
      };

      var waterCompoundId = Guid.NewGuid();
      var ethanolCompoundId = Guid.NewGuid();

      simulation.Compounds.Add(new Compound
      {
         Id = waterCompoundId,
         Name = "Water",
         SimulationId = simulation.Id
      });

      simulation.Compounds.Add(new Compound
      {
         Id = ethanolCompoundId,
         Name = "Ethanol",
         SimulationId = simulation.Id
      });

      var inputStreamId = Guid.NewGuid();
      var outputStream1Id = Guid.NewGuid();
      var outputStream2Id = Guid.NewGuid();

      simulation.MaterialStreams.Add(new MaterialStream
      {
         Id = inputStreamId,
         Name = "Feed",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(1.0),
         Composition = new Dictionary<string, double> { { "Water", 0.5 }, { "Ethanol", 0.5 } }
      });

      simulation.MaterialStreams.Add(new MaterialStream
      {
         Id = outputStream1Id,
         Name = "Distillate",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(0.5),
         Composition = new Dictionary<string, double> { { "Ethanol", 1.0 } }
      });

      simulation.MaterialStreams.Add(new MaterialStream
      {
         Id = outputStream2Id,
         Name = "Bottoms",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(0.5),
         Composition = new Dictionary<string, double> { { "Water", 1.0 } }
      });

      // ShortcutColumn with valid keys
      simulation.UnitOperations.Add(new ShortcutColumnObject
      {
         Id = Guid.NewGuid(),
         Name = "Column1",
         SimulationId = simulation.Id,
         InputStreamIds = new List<Guid> { inputStreamId },
         OutputStreamIds = new List<Guid> { outputStream1Id, outputStream2Id },
         LightKey = ethanolCompoundId, // Valid
         HeavyKey = waterCompoundId, // Valid
         RefluxRatio = 2.0,
         Stages = 10,
         CondenserPressure = 101325,
         ReboilerPressure = 101325
      });

      return simulation;
   }

   private SimulationEntity CreateSimulationWithBothInvalidKeys()
   {
      var simulation = new SimulationEntity
      {
         Id = Guid.NewGuid(),
         Name = "Test Simulation",
         PropertyPackage = PropertyPackageType.PengRobinson,
         FlashAlgorithm = FlashAlgorithm.NestedLoops,
         SystemOfUnits = SystemOfUnits.SI,
         Status = SimulationStatus.Created
      };

      simulation.Compounds.Add(new Compound
      {
         Id = Guid.NewGuid(),
         Name = "Water",
         SimulationId = simulation.Id
      });

      simulation.Compounds.Add(new Compound
      {
         Id = Guid.NewGuid(),
         Name = "Ethanol",
         SimulationId = simulation.Id
      });

      var inputStreamId = Guid.NewGuid();
      var outputStream1Id = Guid.NewGuid();
      var outputStream2Id = Guid.NewGuid();

      simulation.MaterialStreams.Add(new MaterialStream
      {
         Id = inputStreamId,
         Name = "Feed",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(1.0),
         Composition = new Dictionary<string, double> { { "Water", 0.5 }, { "Ethanol", 0.5 } }
      });

      simulation.MaterialStreams.Add(new MaterialStream
      {
         Id = outputStream1Id,
         Name = "Distillate",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(0.5),
         Composition = new Dictionary<string, double> { { "Ethanol", 1.0 } }
      });

      simulation.MaterialStreams.Add(new MaterialStream
      {
         Id = outputStream2Id,
         Name = "Bottoms",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(0.5),
         Composition = new Dictionary<string, double> { { "Water", 1.0 } }
      });

      // ShortcutColumn with both invalid keys
      simulation.UnitOperations.Add(new ShortcutColumnObject
      {
         Id = Guid.NewGuid(),
         Name = "Column1",
         SimulationId = simulation.Id,
         InputStreamIds = new List<Guid> { inputStreamId },
         OutputStreamIds = new List<Guid> { outputStream1Id, outputStream2Id },
         LightKey = Guid.NewGuid(), // Invalid!
         HeavyKey = Guid.NewGuid(), // Invalid!
         RefluxRatio = 2.0,
         Stages = 10,
         CondenserPressure = 101325,
         ReboilerPressure = 101325
      });

      return simulation;
   }

   private SimulationEntity CreateSimulationWithMultipleUndefinedCompounds()
   {
      var simulation = new SimulationEntity
      {
         Id = Guid.NewGuid(),
         Name = "Test Simulation",
         PropertyPackage = PropertyPackageType.PengRobinson,
         FlashAlgorithm = FlashAlgorithm.NestedLoops,
         SystemOfUnits = SystemOfUnits.SI,
         Status = SimulationStatus.Created
      };

      // Define only Water
      simulation.Compounds.Add(new Compound
      {
         Id = Guid.NewGuid(),
         Name = "Water",
         SimulationId = simulation.Id
      });

      // Stream 1 references undefined "Ethanol"
      var stream1Id = Guid.NewGuid();
      simulation.MaterialStreams.Add(new MaterialStream
      {
         Id = stream1Id,
         Name = "Stream1",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(1.0),
         Composition = new Dictionary<string, double>
            {
                { "Water", 0.5 },
                { "Ethanol", 0.5 } // Undefined!
            }
      });

      // Stream 2 references undefined "Methanol"
      var stream2Id = Guid.NewGuid();
      simulation.MaterialStreams.Add(new MaterialStream
      {
         Id = stream2Id,
         Name = "Stream2",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(1.0),
         Composition = new Dictionary<string, double>
            {
                { "Water", 0.5 },
                { "Methanol", 0.5 } // Undefined!
            }
      });

      // Connect to units
      var outputStreamId = Guid.NewGuid();
      simulation.MaterialStreams.Add(new MaterialStream
      {
         Id = outputStreamId,
         Name = "OutputStream",
         SimulationId = simulation.Id,
         Temperature = new Temperature(298.15),
         Pressure = new Pressure(101325),
         MassFlow = new MassFlow(1.0),
         Composition = new Dictionary<string, double>
            {
                { "Water", 1.0 }
            }
      });

      simulation.UnitOperations.Add(new MixerObject
      {
         Id = Guid.NewGuid(),
         Name = "Mixer1",
         SimulationId = simulation.Id,
         InputStreamIds = new List<Guid> { stream1Id, stream2Id },
         OutputStreamIds = new List<Guid> { outputStreamId }
      });

      return simulation;
   }

   #endregion
}

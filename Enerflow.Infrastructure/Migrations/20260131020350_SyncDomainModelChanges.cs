using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enerflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncDomainModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "ThermoPackage",
                table: "Simulations");

            migrationBuilder.RenameColumn(
                name: "MolarCompositions",
                table: "MaterialStreams",
                newName: "Composition");

            migrationBuilder.AddColumn<string>(
                name: "Acceleration",
                table: "UnitOperations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CalcMode",
                table: "UnitOperations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CondenserPressure",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoolerObject_CalcMode",
                table: "UnitOperations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CoolerObject_Efficiency",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CoolerObject_HeatDuty",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CoolerObject_OutletTemperature",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CoolerObject_PressureDrop",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CoolerObject_TemperatureChange",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Efficiency",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FlashDrumObject_OutletTemperature",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlashType",
                table: "UnitOperations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HeatDuty",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HeavyKey",
                table: "UnitOperations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HeavyKeyFraction",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LightKey",
                table: "UnitOperations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LightKeyFraction",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxIterations",
                table: "UnitOperations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OutletPressure",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OutletTemperature",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Position_X",
                table: "UnitOperations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Position_Y",
                table: "UnitOperations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "PressureDrop",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ReboilerPressure",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RefluxRatio",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<Dictionary<Guid, double>>(
                name: "SplitRatios",
                table: "UnitOperations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Stages",
                table: "UnitOperations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TemperatureChange",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Tolerance",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitType",
                table: "UnitOperations",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ValveObject_CalcMode",
                table: "UnitOperations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ValveObject_OutletPressure",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ValveObject_PressureDrop",
                table: "UnitOperations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SystemOfUnits",
                table: "Simulations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Simulations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "FlashAlgorithm",
                table: "Simulations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "PropertyPackage",
                table: "Simulations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Phase",
                table: "MaterialStreams",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MolarFlow",
                table: "MaterialStreams",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Position_X",
                table: "MaterialStreams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Position_Y",
                table: "MaterialStreams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Position_X",
                table: "EnergyStreams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Position_Y",
                table: "EnergyStreams",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Acceleration",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "CalcMode",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "CondenserPressure",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "CoolerObject_CalcMode",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "CoolerObject_Efficiency",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "CoolerObject_HeatDuty",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "CoolerObject_OutletTemperature",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "CoolerObject_PressureDrop",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "CoolerObject_TemperatureChange",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "Efficiency",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "FlashDrumObject_OutletTemperature",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "FlashType",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "HeatDuty",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "HeavyKey",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "HeavyKeyFraction",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "LightKey",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "LightKeyFraction",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "MaxIterations",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "OutletPressure",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "OutletTemperature",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "Position_X",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "Position_Y",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "PressureDrop",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "ReboilerPressure",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "RefluxRatio",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "SplitRatios",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "Stages",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "TemperatureChange",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "Tolerance",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "UnitType",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "ValveObject_CalcMode",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "ValveObject_OutletPressure",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "ValveObject_PressureDrop",
                table: "UnitOperations");

            migrationBuilder.DropColumn(
                name: "PropertyPackage",
                table: "Simulations");

            migrationBuilder.DropColumn(
                name: "MolarFlow",
                table: "MaterialStreams");

            migrationBuilder.DropColumn(
                name: "Position_X",
                table: "MaterialStreams");

            migrationBuilder.DropColumn(
                name: "Position_Y",
                table: "MaterialStreams");

            migrationBuilder.DropColumn(
                name: "Position_X",
                table: "EnergyStreams");

            migrationBuilder.DropColumn(
                name: "Position_Y",
                table: "EnergyStreams");

            migrationBuilder.RenameColumn(
                name: "Composition",
                table: "MaterialStreams",
                newName: "MolarCompositions");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "UnitOperations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "SystemOfUnits",
                table: "Simulations",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Simulations",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "FlashAlgorithm",
                table: "Simulations",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "ThermoPackage",
                table: "Simulations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Phase",
                table: "MaterialStreams",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}

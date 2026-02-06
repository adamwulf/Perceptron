namespace PerceptronSimulator;

/// <summary>
/// Centralized database of electronic components with manufacturer part numbers,
/// specifications, and pricing information. Used by BOM, Schematic, and Breadboard dialogs.
/// </summary>
public static class PartsDatabase
{
    /// <summary>
    /// Represents an electronic component with all relevant specifications.
    /// </summary>
    public class Component
    {
        public string Id { get; init; } = "";
        public string Category { get; init; } = "";
        public string Description { get; init; } = "";
        public string Value { get; init; } = "";
        public string Package { get; init; } = "";
        public string Manufacturer { get; init; } = "";
        public string PartNumber { get; init; } = "";
        public string Supplier { get; init; } = "";
        public double UnitPrice { get; init; }
        public string Notes { get; init; } = "";

        // Electrical specifications
        public double? Resistance { get; init; }      // Ohms
        public double? Capacitance { get; init; }     // Farads
        public double? Voltage { get; init; }         // Volts (rating)
        public double? Current { get; init; }         // Amps (rating)
        public double? Power { get; init; }           // Watts
        public double? Tolerance { get; init; }       // Percentage (e.g., 5 for 5%)
    }

    // === POWER SUPPLY COMPONENTS ===

    public static readonly Component Battery9V = new()
    {
        Id = "PWR-BAT-9V",
        Category = "Power",
        Description = "9V Battery",
        Value = "9V Alkaline",
        Package = "9V Snap",
        Manufacturer = "Duracell",
        PartNumber = "MN1604",
        Supplier = "Amazon/Store",
        UnitPrice = 3.00,
        Voltage = 9.0,
        Notes = "Standard 9V alkaline battery"
    };

    public static readonly Component BatterySnap = new()
    {
        Id = "PWR-SNAP-9V",
        Category = "Power",
        Description = "9V Battery Snap Connector",
        Value = "9V to wire leads",
        Package = "Snap",
        Manufacturer = "Keystone",
        PartNumber = "968",
        Supplier = "DigiKey",
        UnitPrice = 0.50,
        Notes = "Connects 9V battery to circuit"
    };

    public static readonly Component VoltageRegulator5V = new()
    {
        Id = "PWR-REG-7805",
        Category = "Power",
        Description = "Voltage Regulator",
        Value = "+5V 1A TO-220",
        Package = "TO-220",
        Manufacturer = "Texas Instruments",
        PartNumber = "LM7805CT",
        Supplier = "DigiKey/Mouser",
        UnitPrice = 0.65,
        Voltage = 5.0,
        Current = 1.0,
        Notes = "Linear regulator, needs heatsink for high current"
    };

    // === CAPACITORS ===

    public static readonly Component CapElectrolytic10uF = new()
    {
        Id = "CAP-ELEC-10UF",
        Category = "Capacitor",
        Description = "Electrolytic Capacitor",
        Value = "10uF 16V",
        Package = "Radial 5mm",
        Manufacturer = "Nichicon",
        PartNumber = "UVR1C100MDD",
        Supplier = "DigiKey",
        UnitPrice = 0.15,
        Capacitance = 10e-6,
        Voltage = 16.0,
        Notes = "Polarized, observe polarity when installing"
    };

    public static readonly Component CapCeramic100nF = new()
    {
        Id = "CAP-CER-100NF",
        Category = "Capacitor",
        Description = "Ceramic Capacitor",
        Value = "0.1uF 50V",
        Package = "Disc/MLCC",
        Manufacturer = "Kemet",
        PartNumber = "C315C104M5U5TA",
        Supplier = "DigiKey",
        UnitPrice = 0.10,
        Capacitance = 100e-9,
        Voltage = 50.0,
        Notes = "Decoupling capacitor, place close to IC"
    };

    // === INTEGRATED CIRCUITS ===

    public static readonly Component OpAmpLM358 = new()
    {
        Id = "IC-OPAMP-LM358",
        Category = "IC",
        Description = "Dual Op-Amp IC",
        Value = "LM358 DIP-8",
        Package = "DIP-8",
        Manufacturer = "Texas Instruments",
        PartNumber = "LM358N",
        Supplier = "DigiKey/Mouser",
        UnitPrice = 0.55,
        Voltage = 32.0,  // Max supply voltage
        Notes = "Single-supply dual op-amp, rail-to-rail input"
    };

    public static readonly Component DIPSocket8Pin = new()
    {
        Id = "SOCK-DIP8",
        Category = "Socket",
        Description = "8-Pin DIP Socket",
        Value = "Optional but recommended",
        Package = "DIP-8",
        Manufacturer = "Mill-Max",
        PartNumber = "110-44-308-41-001",
        Supplier = "DigiKey",
        UnitPrice = 0.30,
        Notes = "Allows easy IC replacement"
    };

    // === SWITCHES ===

    public static readonly Component SwitchSPDT = new()
    {
        Id = "SW-SPDT-TOGGLE",
        Category = "Switch",
        Description = "SPDT Toggle Switch",
        Value = "Mini toggle ON-ON",
        Package = "Panel/PCB",
        Manufacturer = "E-Switch",
        PartNumber = "100SP1T1B4M2QE",
        Supplier = "DigiKey",
        UnitPrice = 1.50,
        Notes = "Single-pole double-throw for input selection"
    };

    // === RESISTORS ===
    // Standard E24 series resistors used in the perceptron circuit

    public static readonly Component Resistor10k = new()
    {
        Id = "RES-10K",
        Category = "Resistor",
        Description = "Resistor",
        Value = "10k 1/4W 5%",
        Package = "Axial",
        Manufacturer = "Yageo",
        PartNumber = "CFR-25JB-52-10K",
        Supplier = "DigiKey",
        UnitPrice = 0.10,
        Resistance = 10000,
        Power = 0.25,
        Tolerance = 5,
        Notes = "Reference resistor (Rf), voltage divider"
    };

    public static readonly Component Resistor4k7 = new()
    {
        Id = "RES-4K7",
        Category = "Resistor",
        Description = "Resistor",
        Value = "4.7k 1/4W 5%",
        Package = "Axial",
        Manufacturer = "Yageo",
        PartNumber = "CFR-25JB-52-4K7",
        Supplier = "DigiKey",
        UnitPrice = 0.10,
        Resistance = 4700,
        Power = 0.25,
        Tolerance = 5,
        Notes = "Pull-up resistor for comparator output"
    };

    public static readonly Component Resistor470R = new()
    {
        Id = "RES-470R",
        Category = "Resistor",
        Description = "Resistor",
        Value = "470R 1/4W 5%",
        Package = "Axial",
        Manufacturer = "Yageo",
        PartNumber = "CFR-25JB-52-470R",
        Supplier = "DigiKey",
        UnitPrice = 0.10,
        Resistance = 470,
        Power = 0.25,
        Tolerance = 5,
        Notes = "LED current limiting resistor"
    };

    public static readonly Component Resistor220R = new()
    {
        Id = "RES-220R",
        Category = "Resistor",
        Description = "Resistor",
        Value = "220R 1/4W 5%",
        Package = "Axial",
        Manufacturer = "Yageo",
        PartNumber = "CFR-25JB-52-220R",
        Supplier = "DigiKey",
        UnitPrice = 0.10,
        Resistance = 220,
        Power = 0.25,
        Tolerance = 5,
        Notes = "Input LED current limiting resistor"
    };

    // === LEDs ===

    public static readonly Component LEDGreen3mm = new()
    {
        Id = "LED-GRN-3MM",
        Category = "LED",
        Description = "LED (Input indicator)",
        Value = "Green 3mm T-1",
        Package = "T-1 3mm",
        Manufacturer = "Kingbright",
        PartNumber = "WP7113GD",
        Supplier = "DigiKey",
        UnitPrice = 0.15,
        Voltage = 2.2,  // Forward voltage
        Current = 0.020,  // 20mA typical
        Notes = "Input state indicator"
    };

    public static readonly Component LEDGreen5mm = new()
    {
        Id = "LED-GRN-5MM",
        Category = "LED",
        Description = "LED (Output)",
        Value = "Green 5mm T-1 3/4",
        Package = "T-1 3/4 5mm",
        Manufacturer = "Kingbright",
        PartNumber = "WP7113SGD",
        Supplier = "DigiKey",
        UnitPrice = 0.20,
        Voltage = 2.2,
        Current = 0.020,
        Notes = "Main output indicator"
    };

    // === BREADBOARD & WIRING ===

    public static readonly Component Breadboard830 = new()
    {
        Id = "BB-830",
        Category = "Breadboard",
        Description = "Solderless Breadboard",
        Value = "830 tie points",
        Package = "Full size",
        Manufacturer = "BusBoard",
        PartNumber = "BB830",
        Supplier = "DigiKey/Amazon",
        UnitPrice = 6.00,
        Notes = "Standard full-size breadboard"
    };

    public static readonly Component JumperWireKit = new()
    {
        Id = "WIRE-KIT",
        Category = "Wiring",
        Description = "Jumper Wire Kit",
        Value = "M-M assorted colors",
        Package = "22AWG",
        Manufacturer = "Generic",
        PartNumber = "140pc kit",
        Supplier = "Amazon",
        UnitPrice = 8.00,
        Notes = "Male-to-male jumper wires for breadboard"
    };

    // === HELPER METHODS ===

    /// <summary>
    /// E24 standard resistor value multipliers
    /// </summary>
    private static readonly double[] E24Values =
    {
        1.0, 1.1, 1.2, 1.3, 1.5, 1.6, 1.8, 2.0, 2.2, 2.4, 2.7, 3.0,
        3.3, 3.6, 3.9, 4.3, 4.7, 5.1, 5.6, 6.2, 6.8, 7.5, 8.2, 9.1
    };

    /// <summary>
    /// Reference resistor value for weight-to-resistance calculations
    /// </summary>
    public const double ReferenceResistor = 10000.0;

    /// <summary>
    /// Calculates the input resistor value for a given weight.
    /// Rin = Rf / |weight| for inverting summing amplifier.
    /// </summary>
    public static double CalculateInputResistance(double weight)
    {
        if (Math.Abs(weight) < 0.01)
            return 100000; // Very high resistance for near-zero weights
        return ReferenceResistor / Math.Abs(weight);
    }

    /// <summary>
    /// Rounds a resistance value to the nearest E24 standard value.
    /// </summary>
    public static double GetNearestE24Value(double ohms)
    {
        if (ohms < 10) ohms = 10;
        if (ohms > 10000000) ohms = 10000000;

        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(ohms)));
        double normalized = ohms / magnitude;

        double closest = E24Values[0];
        double minDiff = Math.Abs(normalized - closest);

        foreach (var val in E24Values)
        {
            double diff = Math.Abs(normalized - val);
            if (diff < minDiff)
            {
                minDiff = diff;
                closest = val;
            }
        }

        return closest * magnitude;
    }

    /// <summary>
    /// Formats a resistance value as a human-readable string (e.g., "10k", "470R", "1M").
    /// </summary>
    public static string FormatResistorValue(double ohms)
    {
        double standardValue = GetNearestE24Value(ohms);

        if (standardValue >= 1000000)
            return $"{standardValue / 1000000:0.#}M";
        else if (standardValue >= 1000)
            return $"{standardValue / 1000:0.##}k";
        else
            return $"{standardValue:0}R";
    }

    /// <summary>
    /// Parses a resistor value string back to ohms.
    /// </summary>
    public static double ParseResistorValue(string value)
    {
        value = value.ToUpper().Replace(" ", "");
        if (value.EndsWith("M"))
            return double.Parse(value.TrimEnd('M')) * 1000000;
        else if (value.EndsWith("K"))
            return double.Parse(value.TrimEnd('K')) * 1000;
        else if (value.EndsWith("R"))
            return double.Parse(value.TrimEnd('R'));
        else
            return double.Parse(value);
    }

    /// <summary>
    /// Generates a Yageo CFR series part number for a given resistor value string.
    /// </summary>
    public static string GetResistorPartNumber(string valueString)
    {
        string valueCode = valueString.Replace(".", "R").ToUpper();
        if (!valueCode.EndsWith("K") && !valueCode.EndsWith("M") && !valueCode.EndsWith("R"))
            valueCode += "R";

        return $"Yageo CFR-25JB-52-{valueCode}";
    }

    /// <summary>
    /// Creates a Component record for a dynamically calculated input resistor.
    /// </summary>
    public static Component CreateInputResistor(double weight)
    {
        double resistance = CalculateInputResistance(weight);
        string valueStr = FormatResistorValue(resistance);

        return new Component
        {
            Id = $"RES-INPUT-{valueStr}",
            Category = "Resistor",
            Description = "Resistor (Input)",
            Value = $"{valueStr} 1/4W 5%",
            Package = "Axial",
            Manufacturer = "Yageo",
            PartNumber = GetResistorPartNumber(valueStr),
            Supplier = "DigiKey",
            UnitPrice = 0.10,
            Resistance = GetNearestE24Value(resistance),
            Power = 0.25,
            Tolerance = 5,
            Notes = $"Input resistor for weight {weight:F2}"
        };
    }

    /// <summary>
    /// Gets all fixed components needed for any perceptron build (regardless of input count).
    /// </summary>
    public static IEnumerable<(Component Part, int Quantity, string Purpose)> GetFixedComponents()
    {
        yield return (Battery9V, 1, "Power source");
        yield return (BatterySnap, 1, "Battery connection");
        yield return (VoltageRegulator5V, 1, "5V regulation");
        yield return (CapElectrolytic10uF, 1, "Input filter capacitor");
        yield return (CapElectrolytic10uF, 1, "Output filter capacitor");
        yield return (CapCeramic100nF, 2, "Decoupling capacitors");
        yield return (OpAmpLM358, 1, "Summing amplifier + comparator");
        yield return (DIPSocket8Pin, 1, "IC socket (recommended)");
        yield return (Resistor10k, 1, "Feedback resistor (Rf)");
        yield return (Resistor10k, 2, "Virtual ground voltage divider");
        yield return (Resistor4k7, 1, "Output pull-up resistor");
        yield return (Resistor470R, 1, "Output LED current limiter");
        yield return (LEDGreen5mm, 1, "Output indicator");
        yield return (Breadboard830, 1, "Circuit assembly");
        yield return (JumperWireKit, 1, "Interconnections");
    }

    /// <summary>
    /// Gets per-input components (quantity multiplied by input count).
    /// </summary>
    public static IEnumerable<(Component Part, string Purpose)> GetPerInputComponents()
    {
        yield return (SwitchSPDT, "Input selection switch");
        yield return (Resistor220R, "Input LED current limiter");
        yield return (LEDGreen3mm, "Input state indicator");
    }
}

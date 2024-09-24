using System.Text;

namespace MotecLogSerializer.LdParser;

public class LdVenue
{
    public string Name { get; set; }
    public ushort VehiclePtr { get; set; }
    public LdVehicle Vehicle { get; set; }

    public static LdVenue FromFile(BinaryReader reader)
    {
        var name = Encoding.ASCII.GetString(reader.ReadBytes(64)).TrimEnd('\0');
        reader.BaseStream.Seek(1034, SeekOrigin.Current); // Skip 1034 bytes
        var vehiclePtr = reader.ReadUInt16();

        var ldVenue = new LdVenue
        {
            Name = name,
            VehiclePtr = vehiclePtr
        };

        if (vehiclePtr > 0)
        {
            reader.BaseStream.Seek(vehiclePtr, SeekOrigin.Begin);
            ldVenue.Vehicle = LdVehicle.FromFile(reader);
        }

        return ldVenue;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Encoding.ASCII.GetBytes(Name.PadRight(64, '\0')));
        writer.Write(new byte[1034]); // Write 1034 zero bytes
        writer.Write(VehiclePtr);

        if (VehiclePtr > 0)
        {
            writer.Seek(VehiclePtr, SeekOrigin.Begin);
            Vehicle.Write(writer);
        }
    }

    public override string ToString() => $"{Name}; vehicle: {Vehicle}";
}

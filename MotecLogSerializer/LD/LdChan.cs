using System.Runtime.InteropServices;
using System.Text;

namespace MotecLogSerializer.LdParser;

public class LdChan
{
    public string FilePath { get; private set; }
    public uint MetaPtr { get; set; }
    public uint PrevMetaPtr { get; set; }
    public uint NextMetaPtr { get; set; }
    public uint DataPtr { get; set; }
    public uint DataLen { get; set; }
    public Type? DataType { get; set; }
    public ushort Frequency { get; set; }
    public short Shift { get; set; }
    public short Multiplier { get; set; }
    public short Scale { get; set; }
    public short DecimalPlaces { get; set; }
    public string Name { get; set; }
    public string ShortName { get; set; }
    public string Unit { get; set; }

    private float[] _data;

    private LdChan() { }

    public static LdChan FromFile(string filePath, uint metaPtr)
    {
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fileStream);
        reader.BaseStream.Seek(metaPtr, SeekOrigin.Begin);

        var prevMetaPtr = reader.ReadUInt32();
        var nextMetaPtr = reader.ReadUInt32();
        var dataPtr = reader.ReadUInt32();
        var dataLen = reader.ReadUInt32();
        var counter = reader.ReadUInt16();
        var dataTypeA = reader.ReadUInt16();
        var dataType = reader.ReadUInt16();
        var frequency = reader.ReadUInt16();
        var shift = reader.ReadInt16();
        var multiplier = reader.ReadInt16();
        var scale = reader.ReadInt16();
        var decimalPlaces = reader.ReadInt16();
        var name = Encoding.ASCII.GetString(reader.ReadBytes(32)).TrimEnd('\0');
        var shortName = Encoding.ASCII.GetString(reader.ReadBytes(8)).TrimEnd('\0');
        var unit = Encoding.ASCII.GetString(reader.ReadBytes(12)).TrimEnd('\0');

        return new LdChan
        {
            FilePath = filePath,
            MetaPtr = metaPtr,
            PrevMetaPtr = prevMetaPtr,
            NextMetaPtr = nextMetaPtr,
            DataPtr = dataPtr,
            DataLen = dataLen,
            DataType = GetDataType(dataTypeA, dataType),
            Frequency = frequency,
            Shift = shift,
            Multiplier = multiplier,
            Scale = scale,
            DecimalPlaces = decimalPlaces,
            Name = name,
            ShortName = shortName,
            Unit = unit
        };
    }

    public void Write(BinaryWriter writer, int channelIndex)
    {
        ushort dataTypeA;
        ushort dataType;

        if (DataType == typeof(float))
        {
            dataTypeA = 0x07;
            dataType = DataType == typeof(float) ? (ushort)4 : (ushort)2;
        }
        else
        {
            dataTypeA = DataType == typeof(int) ? (ushort)0x05 : (ushort)0x03;
            dataType = DataType == typeof(int) ? (ushort)4 : (ushort)2;
        }

        writer.Write(PrevMetaPtr);
        writer.Write(NextMetaPtr);
        writer.Write(DataPtr);
        writer.Write(DataLen);
        writer.Write((ushort)(0x2ee1 + channelIndex));
        writer.Write(dataTypeA);
        writer.Write(dataType);
        writer.Write(Frequency);
        writer.Write(Shift);
        writer.Write(Multiplier);
        writer.Write(Scale);
        writer.Write(DecimalPlaces);
        writer.Write(Encoding.ASCII.GetBytes(Name.PadRight(32, '\0')));
        writer.Write(Encoding.ASCII.GetBytes(ShortName.PadRight(8, '\0')));
        writer.Write(Encoding.ASCII.GetBytes(Unit.PadRight(12, '\0')));
        writer.Write(new byte[40]); // Write 40 zero bytes
    }

    public float[] Data
    {
        get
        {
            if (_data == null)
            {
                if (DataType == null)
                    return [];

                using var fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
                using var reader = new BinaryReader(fileStream);
                reader.BaseStream.Seek(DataPtr, SeekOrigin.Begin);
                var rawData = new byte[DataLen * Marshal.SizeOf(DataType)];
                reader.Read(rawData, 0, rawData.Length);

                _data = new float[DataLen];
                Buffer.BlockCopy(rawData, 0, _data, 0, rawData.Length);

                for (int i = 0; i < _data.Length; i++)
                {
                    _data[i] = (_data[i] / Scale * (float)Math.Pow(10, -DecimalPlaces) + Shift) * Multiplier;
                }
            }
            return _data;
        }
    }

    public override string ToString() =>
        $"chan {Name} ({ShortName}) [{Unit}], {Frequency} Hz";

    private static Type? GetDataType(ushort dataTypeA, ushort dataType)
    {
        if (dataTypeA == 0x07)
        {
            return dataType switch
            {
                2 => typeof(float),
                4 => typeof(float),
                _ => null
            };
        }
        else if (dataTypeA == 0 || dataTypeA == 0x03 || dataTypeA == 0x05)
        {
            return dataType switch
            {
                2 => typeof(short),
                4 => typeof(int),
                _ => null
            };
        }

        return null;
    }

    public byte[] ConvertData()
    {
        var convertedData = new byte[DataLen * Marshal.SizeOf(DataType)];
        var floatData = new float[DataLen];

        for (int i = 0; i < DataLen; i++)
        {
            floatData[i] = ((Data[i] / Multiplier) - Shift) * Scale * (float)Math.Pow(10, DecimalPlaces);
        }

        Buffer.BlockCopy(floatData, 0, convertedData, 0, convertedData.Length);
        return convertedData;
    }
}

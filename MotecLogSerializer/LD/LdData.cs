namespace MotecLogSerializer.LdParser;

public class LdData(LdHead head, List<LdChan> channels)
{
    public LdHead Head { get; private set; } = head;
    public List<LdChan> Channels { get; private set; } = channels;

    public LdChan this[int index] => Channels[index];

    public LdChan this[string name] => Channels.FirstOrDefault(c => c.Name == name);

    public static LdData FromFile(string filePath)
    {
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        LdHead head = LdHead.FromFile(fileStream);
        List<LdChan> channels = LdParser.ReadChannels(filePath, head.MetaPtr);
        return new LdData(head, channels);
    }


    public void Write(string filePath)
    {
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(fileStream);
        Head.Write(writer, Channels.Count);
        writer.Seek((int)Channels[0].MetaPtr, SeekOrigin.Begin);

        for (int i = 0; i < Channels.Count; i++)
        {
            Channels[i].Write(writer, i);
        }

        foreach (LdChan channel in Channels)
        {
            byte[] convertedData = channel.ConvertData();
            writer.Write(convertedData);
        }

    }
}
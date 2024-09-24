using System.Text;

namespace MotecLogSerializer.LdParser;

public class LdEvent
{
    public string Name { get; set; }
    public string Session { get; set; }
    public string Comment { get; set; }
    public ushort VenuePtr { get; set; }
    public LdVenue Venue { get; set; }

    public static LdEvent FromFile(BinaryReader reader)
    {
        var name = Encoding.ASCII.GetString(reader.ReadBytes(64)).TrimEnd('\0');
        var session = Encoding.ASCII.GetString(reader.ReadBytes(64)).TrimEnd('\0');
        var comment = Encoding.ASCII.GetString(reader.ReadBytes(1024)).TrimEnd('\0');
        var venuePtr = reader.ReadUInt16();

        var ldEvent = new LdEvent
        {
            Name = name,
            Session = session,
            Comment = comment,
            VenuePtr = venuePtr
        };

        if (venuePtr > 0)
        {
            reader.BaseStream.Seek(venuePtr, SeekOrigin.Begin);
            ldEvent.Venue = LdVenue.FromFile(reader);
        }

        return ldEvent;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Encoding.ASCII.GetBytes(Name.PadRight(64, '\0')));
        writer.Write(Encoding.ASCII.GetBytes(Session.PadRight(64, '\0')));
        writer.Write(Encoding.ASCII.GetBytes(Comment.PadRight(1024, '\0')));
        writer.Write(VenuePtr);

        if (VenuePtr > 0)
        {
            writer.Seek(VenuePtr, SeekOrigin.Begin);
            Venue.Write(writer);
        }
    }

    public override string ToString() => $"{Name}; venue: {Venue}";
}

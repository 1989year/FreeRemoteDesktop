public class CustomAuthTicket
{
    public DateTime Expired { get; set; }

    public byte[] Sign { get; set; }
}
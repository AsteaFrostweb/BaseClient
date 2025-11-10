using BaseClient;


public  class Program 
{

    public static async Task Main(string[] args) 
    {
        Client c = new Client("127.0.0.1", 58585);
        await c.RunAsync();
    
    }
}
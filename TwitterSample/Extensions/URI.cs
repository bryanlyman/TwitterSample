namespace System
{
    public static partial class Extensions
    {
        public static string UrlAndPort(this Uri uri)
        {
            return uri.Scheme + "://" + uri.Host + (uri.Port != 80 ? $":{uri.Port}" : "");
        }
    }
}

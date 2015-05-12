using System.Collections.Specialized;

namespace PushAkka.Core.Messages
{
    public class WindowsPhoneToast : BaseWindowsPhonePushMessage
    {
        public string Text1 { get; set; }
        public string Text2 { get; set; }
        public string NavigatePath { get; set; }
        public NameValueCollection Parameters { get; set; }
    }
}
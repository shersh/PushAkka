using System.Collections.Specialized;

namespace PushAkka.Core.Messages
{

    /// <summary>
    /// 
    /// </summary>
    public class WindowsPhoneToast : BaseWindowsPhonePushMessage
    {
        public string Text1 { get; set; }
        public string Text2 { get; set; }
        public string NavigatePath { get; set; }
        public NameValueCollection Parameters { get; set; }
    }


    /// <summary>
    /// https://msdn.microsoft.com/en-us/library/windows/apps/hh202970(v=vs.105).aspx
    /// </summary>
    public class WindowsPhoneTile : BaseWindowsPhonePushMessage
    {
        public string BackgroundImage { get; set; }
        public string BackBackgroundImage { get; set; }
        public string Title { get; set; }
        public string BackTitle { get; set; }
        public string BackContent { get; set; }
        public int Count { get; set; }
    }


    /// <summary>
    /// https://msdn.microsoft.com/ru-ru/library/windows/apps/hh202977(v=vs.105).aspx
    /// </summary>
    public class WindowsPhoneRaw : BaseWindowsPhonePushMessage
    {
        public string Value1 { get; set; }
        public string Value2 { get; set; }
    }
}
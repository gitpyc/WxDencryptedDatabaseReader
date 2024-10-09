using ESFramework.Extensions.ChatRendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wxreader
{
    public class wxmessageprovider : IRenderDataProvider
    {
        public Dictionary<int, object> GetEmotions()
        {
            throw new NotImplementedException();
        }

        public string GetFilePathToSave(string fileName)
        {
            throw new NotImplementedException();
        }

        public object GetImageOfAudioCall()
        {
            throw new NotImplementedException();
        }

        public object GetImageOfFileType(string fileExtendName)
        {
            throw new NotImplementedException();
        }

        public object GetImageOfSendFailed()
        {
            throw new NotImplementedException();
        }

        public object GetImageOfVideoCall()
        {
            throw new NotImplementedException();
        }

        public string GetSpeakerDisplayName(string speakerID)
        {
            throw new NotImplementedException();
        }

        public object GetUserHeadImage(string userID)
        {
            throw new NotImplementedException();
        }

        public string GetUserName(string userID)
        {
            throw new NotImplementedException();
        }
    }
}

#define SOCIAL_API
////////////////////////////////////////////////////////////////////////////////
//  
// @module IOS Native Plugin
// @author Osipov Stanislav (Stan's Assets) 
// @support support@stansassets.com
// @website https://stansassets.com
//
////////////////////////////////////////////////////////////////////////////////

using System;
using UnityEngine;
using System.Collections.Generic;
#if (UNITY_IPHONE && !UNITY_EDITOR && SOCIAL_API) || SA_DEBUG_MODE
using System.Runtime.InteropServices;
#endif

using SA.Common.Data;
using SA.Common.Pattern;


namespace SA.IOSNative.Social
{

    public static class TextMessage
    {

        //--------------------------------------
        //  PUBLIC METHODS
        //--------------------------------------


        public static event Action<TextMessageComposeResult> OnTextMessageResult = delegate { };



        public static void Send(string body, string recipient, Action<TextMessageComposeResult> callback = null) {
            Send(body, new string[] { recipient },  new Texture2D[] { }, callback);
        }

        public static void Send(string body, string recipient, Texture2D image, Action<TextMessageComposeResult> callback = null) {
            Send(body, new string[] { recipient }, new Texture2D[] { image }, callback);
        }


        public static void Send(string body, string recipient, Texture2D[] images, Action<TextMessageComposeResult> callback = null) {
            Send(body, new string[] { recipient }, images, callback);
        }


        public static void Send(string body, string[] recipients, Action<TextMessageComposeResult> callback = null) {
            Send(body, recipients, new Texture2D[] { }, callback);
        }

        public static void Send(string body, string[] recipients, Texture2D image, Action<TextMessageComposeResult> callback = null) {
            Send(body, recipients, new Texture2D[] {image}, callback);
        }


        public static void Send(string body, string[] recipients, Texture2D[] images, Action<TextMessageComposeResult> callback = null) {

            if (body == null)           { body = string.Empty; }
            if (recipients == null)     { recipients = new string[] { }; }
            if (images == null)         { images = new Texture2D[] { }; }

            if (callback != null) {
                OnTextMessageResult += callback;
            }



            string encodedRecipients = Converter.SerializeArray(recipients);

            List<string> media = new List<string>();
            foreach (Texture2D image in images) {
                byte[] val = image.EncodeToPNG();
                media.Add(Convert.ToBase64String(val));
            }
            string encodedMedia = Converter.SerializeArray(media.ToArray());



            Internal.ISN_SendTextMessage(body, encodedRecipients, encodedMedia);
        }


        //--------------------------------------
        //  SUPPORT CLASSES
        //--------------------------------------


        private class NativeListener : Singleton<NativeListener> {

            private void OnTextMessageComposeResult(string data) {
                int code = Convert.ToInt32(data);

                OnTextMessageResult((TextMessageComposeResult)code);
                OnTextMessageResult = delegate { };

            }
        }



        private static class Internal {


            #if (UNITY_IPHONE && !UNITY_EDITOR && SOCIAL_API) || SA_DEBUG_MODE
            [DllImport ("__Internal")]
            private static extern void _ISN_SendTextMessage(string body, string recipients, string encodedMedia);
            #endif

            public static void ISN_SendTextMessage(string body, string recipients, string encodedMedia) {
                #if (UNITY_IPHONE && !UNITY_EDITOR && SOCIAL_API) || SA_DEBUG_MODE
                _ISN_SendTextMessage( body, recipients, encodedMedia);
                #endif
            }

        }



    }
}

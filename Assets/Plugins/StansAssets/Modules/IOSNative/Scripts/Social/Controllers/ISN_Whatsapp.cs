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
using SA.Common.Models;


namespace SA.IOSNative.Social
{

    public static class Whatsapp
    {

        //--------------------------------------
        //  PUBLIC METHODS
        //--------------------------------------


        public static void Post(string message) {
            Internal.ISN_WP_ShareText(message);
        }

        public static void Post(Texture2D image) {
            byte[] val = image.EncodeToPNG();
            string encodedMedia = Convert.ToBase64String(val);

            Internal.ISN_WP_ShareMedia(encodedMedia);
        }


        //--------------------------------------
        //  SUPPORT CLASSES
        //--------------------------------------



        private static class Internal {

            #if (UNITY_IPHONE && !UNITY_EDITOR && SOCIAL_API) || SA_DEBUG_MODE
            [DllImport ("__Internal")]
            private static extern void _ISN_WP_ShareText(string message);

            [DllImport ("__Internal")]
            private static extern void _ISN_WP_ShareMedia(string encodedMedia);

            #endif

            public static void ISN_WP_ShareText(string message) {
                #if (UNITY_IPHONE && !UNITY_EDITOR && SOCIAL_API) || SA_DEBUG_MODE
                ISN_WP_ShareText(message);
                #endif
            }

            public static void ISN_WP_ShareMedia(string encodedMedia) {
                #if (UNITY_IPHONE && !UNITY_EDITOR && SOCIAL_API) || SA_DEBUG_MODE
                _ISN_WP_ShareMedia(encodedMedia);
                #endif
            }
        }



    }
}

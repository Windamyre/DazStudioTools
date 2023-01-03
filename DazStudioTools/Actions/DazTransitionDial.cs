using BarRaider.SdTools;
using BarRaider.SdTools.Payloads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DazStudioTools
{
    //todo Make error message visible in PI when unable to communicate with Daz Plugin

    [PluginActionId("com.windamyre.dazstudiotools.daztransition")]

    public class DazTransitionDial : EncoderBase
    {
        #region PluginSettingsClass
        private class PluginSettings
        {
            public enum TransitionType { Translate, Rotate, Scale };
            public enum TransitionAxis { X, Y, Z, All };
            public static PluginSettings CreateDefaultSettings()
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, "Creating Default Settings");
                PluginSettings instance = new PluginSettings();

                //instance.RotateX = true;
                //instance.RotateY = false;
                //instance.RotateZ = false;

                instance.transitionType = TransitionType.Rotate;
                instance.transitionAxis = TransitionAxis.X;
                return instance;
            }
            [JsonProperty(PropertyName = "transitionType")]
            public TransitionType transitionType { get; set; }

            [JsonProperty(PropertyName = "transitionAxis")]
            public TransitionAxis transitionAxis { get; set; }

            //[JsonProperty(PropertyName = "RotateX")]
            //public bool RotateX { get; set; }
            //[JsonProperty(PropertyName = "RotateY")]
            //public bool RotateY { get; set; }
            //[JsonProperty(PropertyName = "RotateZ")]
            //public bool RotateZ { get; set; }

        }

        #endregion
        #region Private Members
        private PluginSettings pluginSettings;
        #endregion

        #region StreamDeckEvents
        public DazTransitionDial(ISDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Constructor Loaded. ");

            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.pluginSettings = PluginSettings.CreateDefaultSettings();
                //ActionListing.LoadActions();
                SaveSettings();
            }
            else
            {
                this.pluginSettings = payload.Settings.ToObject<PluginSettings>();
            }
        }

        public override void DialPress(DialPressPayload payload)
        {

        }
        public override void DialRotate(DialRotatePayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Dial Rotated" + payload.Ticks.ToString());
            string RequestString = $"http://localhost:8080/";
            RequestString += (payload.IsDialPressed ? "setSensitivity/" : "transform/");
            RequestString += pluginSettings.transitionType.ToString();
            switch (pluginSettings.transitionAxis)
            {
                case PluginSettings.TransitionAxis.X:
                    RequestString += $"_{payload.Ticks.ToString()}_0_0";
                    break;
                case PluginSettings.TransitionAxis.Y:
                    RequestString += $"_0_{payload.Ticks.ToString()}_0";
                    break;
                case PluginSettings.TransitionAxis.Z:
                    RequestString += $"_0_0_{payload.Ticks.ToString()}";
                    break;
                default:
                    RequestString += "_{payload.Ticks.ToString()}_{payload.Ticks.ToString()}_{payload.Ticks.ToString()}";
                    break;
            }
              

            //RequestString += "_" + (pluginSettings.RotateX ? payload.Ticks.ToString() : "0");
            //RequestString += "_" + (pluginSettings.RotateY ? payload.Ticks.ToString() : "0");
            //RequestString += "_" + (pluginSettings.RotateZ ? payload.Ticks.ToString() : "0");
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(RequestString);
                var response = req.GetResponse();
                string webContent;
                using (var strm = new StreamReader(response.GetResponseStream()))
                {
                    Dictionary<string, string> feedback = new Dictionary<string, string>();
                    webContent = strm.ReadToEnd();
                    feedback = JsonConvert.DeserializeObject<Dictionary<string, string>>(webContent); switch (pluginSettings.transitionAxis)
                    {
                        case PluginSettings.TransitionAxis.X:
                            RequestString += $"_{payload.Ticks.ToString()}_0_0";
                            break;
                        case PluginSettings.TransitionAxis.Y:
                            RequestString += $"_0_{payload.Ticks.ToString()}_0";
                            break;
                        case PluginSettings.TransitionAxis.Z:
                            RequestString += $"_0_0_{payload.Ticks.ToString()}";
                            break;
                        default:
                            RequestString += "_{payload.Ticks.ToString()}_{payload.Ticks.ToString()}_{payload.Ticks.ToString()}";
                            break;
                    }
                    String feedbackTitle, feedbackValue, feedbackPercent;
                    feedback.TryGetValue("Label", out feedbackTitle);
                    feedback.TryGetValue( pluginSettings.transitionAxis.ToString() + "-Value", out feedbackValue);
                    feedback.TryGetValue("X-Percent", out feedbackPercent);
                    Connection.SetFeedbackAsync(new Dictionary<string, string>() { { "title", feedbackTitle }, { "value", feedbackValue } });
                    //Connection.SetFeedbackAsync(new Dictionary<string, string>() { { "indicator", feedbackPercent } });
                   
                }
                //this.settings.IsDazLoaded = true;
            }
            catch (WebException wEx)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, "Cannot communicate with Daz Studio or plug-in");
                Logger.Instance.LogMessage(TracingLevel.DEBUG, wEx.Message);
                //this.settings.IsDazLoaded = false;
            }
            catch
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Unhandled error in DialRotate event");
            }
        }

        public override void Dispose()
        {
            // throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override void OnTick()
        {
            // throw new NotImplementedException();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
            // throw new NotImplementedException();
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "Received Dial Settings");
            Logger.Instance.LogMessage(TracingLevel.DEBUG, payload.Settings.ToString());
            Tools.AutoPopulateSettings(pluginSettings, payload.Settings);
            SaveSettings();


        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override void TouchPress(TouchpadPressPayload payload)
        {
            if (!payload.IsLongPress) return;
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "TouchPress" + payload.ToString());
            Logger.Instance.LogMessage(TracingLevel.INFO, "Dial Press");
            string RequestString = $"http://localhost:8080/toDefault/";
            RequestString += pluginSettings.transitionType.ToString();
            switch (pluginSettings.transitionAxis)
            {
                case PluginSettings.TransitionAxis.X:
                    RequestString += $"_Y_N_N";
                    break;
                case PluginSettings.TransitionAxis.Y:
                    RequestString += $"_N_Y_N";
                    break;
                case PluginSettings.TransitionAxis.Z:
                    RequestString += $"_N_N_Y";
                    break;
                default:
                    RequestString += "_Y_Y_Y";
                    break;
            }
            //RequestString += "_" + (pluginSettings.RotateX ? "Y" : "N");
            //RequestString += "_" + (pluginSettings.RotateY ? "Y" : "N");
            //RequestString += "_" + (pluginSettings.RotateZ ? "Y" : "N");
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(RequestString);
                var response = req.GetResponse();
                string webcontent;
                using (var strm = new StreamReader(response.GetResponseStream()))
                {
                    webcontent = strm.ReadToEnd();
                }
                //this.settings.IsDazLoaded = true;
            }
            catch (WebException wEx)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, "Cannot communicate with Daz Studio or plug-in");
                Logger.Instance.LogMessage(TracingLevel.DEBUG, wEx.Message);
                //this.settings.IsDazLoaded = false;
            }
            catch
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Unhandled error in DialPress event");
            }
        }
        #endregion

        private Task SaveSettings()
        {
            Logger.Instance.LogMessage(TracingLevel.DEBUG, "Saved Settings");
            return Connection.SetSettingsAsync(JObject.FromObject(pluginSettings));
        }
    }
}

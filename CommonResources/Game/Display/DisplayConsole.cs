﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PacificEngine.OW_CommonResources.Game.Display
{
    public enum ConsoleLocation
    {
        TopLeft,
        TopCenter,
        TopRight,
        CenterLeft,
        Center,
        CenterRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    public class DisplayConsole
    {
        private static Dictionary<ConsoleLocation, DisplayConsole> _locations = new Dictionary<ConsoleLocation, DisplayConsole>();

        public static DisplayConsole getConsole(ConsoleLocation location)
        {
            DisplayConsole console;
            if (_locations.TryGetValue(location, out console))
            {
                return console;
            }
            console = new DisplayConsole(location);
            _locations.Add(location, console);
            return console;
        }

        public static void OnGUI()
        {
            foreach(var console in _locations.Values)
            {
                console.onGUI();
            }
        }

        private float heightPerElement = 20f;
        private float width = 400f;
        private Dictionary<string, Tuple<float, string>> _elements = new Dictionary<string, Tuple<float, string>>();
        private ConsoleLocation _location;

        private DisplayConsole(ConsoleLocation location)
        {
            _location = location;
        }

        public void setElement(string id, string value, float priority)
        {
            if (value.Length == 0)
            {
                _elements.Remove(id);
            }
            else
            {
                _elements[id] = Tuple.Create(priority, value);
            }
        }

        public void onGUI()
        {
            var elements = _elements.ToList();
            elements.Sort((x1, x2) => {
                var comp = x1.Value.Item1.CompareTo(x2.Value.Item1);
                return comp != 0 ? comp : x1.Key.CompareTo(x2.Key);
            });

            float y = 0f;
            float x = 0f;
            switch (_location)
            {
                case ConsoleLocation.CenterLeft:
                case ConsoleLocation.Center:
                case ConsoleLocation.CenterRight:
                    y = ((float)Screen.height / 2f) - ((heightPerElement * (float)elements.Count) / 2f);
                    break;
                case ConsoleLocation.BottomLeft:
                case ConsoleLocation.BottomCenter:
                case ConsoleLocation.BottomRight:
                    y = ((float)Screen.height) - (heightPerElement * (float)elements.Count);
                    break;
            }
            switch (_location)
            {
                case ConsoleLocation.TopCenter:
                case ConsoleLocation.Center:
                case ConsoleLocation.BottomCenter:
                    x = ((float)Screen.width / 2f) - (width / 2f);
                    break;
                case ConsoleLocation.TopRight: 
                case ConsoleLocation.CenterRight:
                case ConsoleLocation.BottomRight:
                    x = ((float)Screen.width) - width;
                    break;
            }

            float i = 0f;
            foreach (var element in elements)
            {
                GUI.Label(new Rect(x, y + (heightPerElement * i++), width, heightPerElement), element.Value.Item2);
            }
        }

        public static string logVector(Vector3? vector)
        {
            return !vector.HasValue ? "" : logVector(vector.Value);
        }

        public static string logVector(Vector3 vector)
        {
            return vector == null ? "" : $"({Math.Round(vector.x, 7).ToString("G7")},{Math.Round(vector.y, 7).ToString("G7")},{Math.Round(vector.z, 7).ToString("G7")})";
        }

        public static string logQuaternion(Quaternion? quaternion)
        {
            return !quaternion.HasValue ? "" : logQuaternion(quaternion.Value);
        }

        public static string logQuaternion(Quaternion quaternion)
        {
            return quaternion == null ? "" : $"({Math.Round(quaternion.x, 3).ToString("G3")},{Math.Round(quaternion.y, 3).ToString("G3")},{Math.Round(quaternion.z, 3).ToString("G3")},{Math.Round(quaternion.w, 3).ToString("G3")})";
        }
    }
}

﻿
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Entities.Editor
{
    public class ComponentGroupGUIControl
    {
        private bool archetypeQueryMode;
        
        private List<GUIStyle> styles;
        private List<GUIContent> names;
        private List<Rect> rects;
        private float height;
        private float width;

        public float Height
        {
            get { return height; }
        }

        public ComponentGroupGUIControl(IEnumerable<ComponentType> types, bool archetypeQueryMode)
        {
            this.archetypeQueryMode = archetypeQueryMode;
            CalculateDrawingParts(types);
        }
        
        void CalculateDrawingParts(IEnumerable<ComponentType> types)
        {
            var typeList = types.ToList();
            typeList.Sort((Comparison<ComponentType>) ComponentGroupGUI.CompareTypes);
            styles = new List<GUIStyle>(typeList.Count);
            names = new List<GUIContent>(typeList.Count);
            rects = new List<Rect>(typeList.Count);
            foreach (var type in typeList)
            {
                var style = ComponentGroupGUI.StyleForAccessMode(type.AccessModeType, archetypeQueryMode);
                var content = new GUIContent((string) ComponentGroupGUI.SpecifiedTypeName(type.GetManagedType()));

                styles.Add(style);
                names.Add(content);
            }
        }
        
        public void OnGUI(Vector2 position)
        {
            if (Event.current.type == EventType.Repaint)
            {
                for (var i = 0; i < rects.Count; ++i)
                {
                    var typeRect = rects[i];
                    typeRect.position += position;
                    styles[i].Draw(typeRect, names[i], false, false, false, false);
                }
            }
        }

        public void OnGUILayout(float width)
        {
            if (this.width != width)
                UpdateSize(width);
            var rect = GUILayoutUtility.GetRect(width, height);
            OnGUI(rect.position);
        }

        public void UpdateSize(float newWidth)
        {
            width = newWidth;
            
            rects.Clear();
            var x = 0f;
            var y = 0f;
            for (var i = 0; i < styles.Count; ++i)
            {
                var rect = new Rect(new Vector2(x, y), styles[i].CalcSize(names[i]));
                if (rect.xMax > width && x != 0f)
                {
                    rect.x = 0f;
                    rect.y += rect.height + 2f;
                }

                x = rect.xMax + 2f;
                y = rect.y;

                rects.Add(rect);
            }

            height = rects.Last().yMax;
        }
    }
}

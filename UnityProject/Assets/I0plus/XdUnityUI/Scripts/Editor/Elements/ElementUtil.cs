﻿using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
#if ODIN_INSPECTOR
using Sirenix.Utilities;
#endif
using Object = UnityEngine.Object;

namespace XdUnityUI.Editor
{
    public static class ElementUtil
    {
        private static TextAnchor? GetChildAlignment(Dictionary<string, object> layoutJson)
        {
            if (!layoutJson.ContainsKey("child_alignment")) return null;
            var childAlignment = layoutJson.Get("child_alignment");

            childAlignment = childAlignment.ToLower();
            if (childAlignment.Contains("upper"))
            {
                if (childAlignment.Contains("left"))
                {
                    return TextAnchor.UpperLeft;
                }

                if (childAlignment.Contains("right"))
                {
                    return TextAnchor.UpperRight;
                }

                if (childAlignment.Contains("center"))
                {
                    return TextAnchor.UpperCenter;
                }

                Debug.LogError("ChildAlignmentが設定できませんでした");
            }
            else if (childAlignment.Contains("middle"))
            {
                if (childAlignment.Contains("left"))
                {
                    return TextAnchor.MiddleLeft;
                }

                if (childAlignment.Contains("right"))
                {
                    return TextAnchor.MiddleRight;
                }

                if (childAlignment.Contains("center"))
                {
                    return TextAnchor.MiddleCenter;
                }

                Debug.LogError("ChildAlignmentが設定できませんでした");
            }
            else if (childAlignment.Contains("lower"))
            {
                if (childAlignment.Contains("left"))
                {
                    return TextAnchor.LowerLeft;
                }

                if (childAlignment.Contains("right"))
                {
                    return TextAnchor.LowerRight;
                }

                if (childAlignment.Contains("center"))
                {
                    return TextAnchor.LowerCenter;
                }

                Debug.LogError("ChildAlignmentが設定できませんでした");
            }

            return null;
        }

        public static T FindComponentByClassName<T>(
            List<Tuple<GameObject, Element>> children,
            string className
        )
        {
            var component = default(T);
            var found = children.Find(child =>
            {
                // StateNameがNULLなら、ClassNameチェックなし
                var (gameObject, element) = child;
                if (className == null || element.HasParsedName(className))
                {
                    component = gameObject.GetComponent<T>();
                    if (component != null) return true;
                }

                if (element is GroupElement groupElement)
                {
                    component = FindComponentByClassName<T>(groupElement.RenderedChildren, className);
                    if (component != null) return true;
                }

                return false;
            });
            return component;
        }

        /**
         * 子供の中にImageComponent化するものが無いか検索し、追加する
         */
        public static Image SetupChildImageComponent(GameObject gameObject,
            List<Tuple<GameObject, Element>> createdChildren)
        {
            // コンポーネント化するImageをもっているオブジェクトを探す
            Tuple<GameObject, Element> childImageBeComponent = null;
            // imageElementを探し､それがコンポーネント化のオプションをもっているか検索
            foreach (var createdChild in createdChildren)
            {
                //TODO: item1がDestroyされていれば、コンティニューの処理が必要
                if (!(createdChild.Item2 is ImageElement)) continue;
                var imageElement = (ImageElement) createdChild.Item2;
                if (imageElement.ComponentJson == null) continue;
                childImageBeComponent = createdChild;
            }

            // イメージコンポーネント化が見つかった場合､それのSpriteを取得し､設定する
            Image goImage = null;
            if (childImageBeComponent != null)
            {
                var imageComponent = childImageBeComponent.Item1.GetComponent<Image>();
                goImage = gameObject.AddComponent<Image>();
                goImage.sprite = imageComponent.sprite;
                goImage.type = imageComponent.type;

                // Spriteを取得したあと､必要ないため削除
                Object.DestroyImmediate(childImageBeComponent.Item1);
            }

            createdChildren.Remove(childImageBeComponent);

            return goImage;
        }

        public static void SetupFillColor(GameObject go, string fillColor)
        {
            // 背景のフィルカラー
            if (fillColor != null)
            {
                var image = go.AddComponent<Image>();
                Color color;
                if (ColorUtility.TryParseHtmlString(fillColor, out color))
                {
                    image.color = color;
                }
            }
        }

        public static ContentSizeFitter SetupContentSizeFitter(GameObject go,
            Dictionary<string, object> contentSizeFitter)
        {
            var componentContentSizeFitter = go.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter == null) return componentContentSizeFitter; // 引数がNULLでも持っていたら返す

            if (componentContentSizeFitter == null)
            {
                componentContentSizeFitter = go.AddComponent<ContentSizeFitter>();
            }

            if (contentSizeFitter.ContainsKey("vertical_fit"))
            {
                var verticalFit = contentSizeFitter.Get("vertical_fit");
                if (verticalFit.Contains("preferred"))
                {
                    componentContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }

                if (verticalFit.Contains("min"))
                {
                    componentContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
                }
            }

            if (contentSizeFitter.ContainsKey("horizontal_fit"))
            {
                var verticalFit = contentSizeFitter.Get("horizontal_fit");
                if (verticalFit.Contains("preferred"))
                {
                    componentContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                }

                if (verticalFit.Contains("min"))
                {
                    componentContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
                }
            }

            return componentContentSizeFitter;
        }

#if ODIN_INSPECTOR
        public static Tuple<MemberInfo, object, Type> GetProperty(Type type, object target, string propertyPath)
        {
            // 参考サイト：　https://stackoverflow.com/questions/366332/best-way-to-get-sub-properties-using-getproperty
            // 配列、Dictionaryへのアクセスも考慮してある
            try
            {
                if (String.IsNullOrEmpty(propertyPath))
                    return null;
                string[] splitter = {"."};
                var sourceProperties = propertyPath.Split(splitter, StringSplitOptions.None);

                //TODO: さすがにfor内に入れるべき
                var infos = type.GetMember(sourceProperties[0]);
                if (infos.Length == 0) return null;
                type = infos[0].GetReturnType();
                var targetHolder = target;
                target = infos[0].GetMemberValue(target);

                // ドットで区切られたメンバー名配列で深堀りしていく
                for (var x = 1; x < sourceProperties.Length; ++x)
                {
                    infos = type.GetMember(sourceProperties[x]);
                    if (infos.Length == 0) return null;
                    type = infos[0].GetReturnType();
                    targetHolder = target;
                    target = infos[0].GetMemberValue(target);
                    var atyperef = __makeref(targetHolder);
                }

                // 値の変更方法
                // Item1.SetMemberValue(Item2, "Cartoon Blip"); 
                return new Tuple<MemberInfo, object, Type>(infos[0], targetHolder, type);
            }
            catch
            {
                throw;
            }
        }

        public static object SetProperty(Type targetType, object targetValue, string propertyPath,
            List<object> strData)
        {
            // 参考サイト：　https://stackoverflow.com/questions/366332/best-way-to-get-sub-properties-using-getproperty
            // 配列、Dictionaryへのアクセスも考慮してある
            try
            {
                if (String.IsNullOrEmpty(propertyPath))
                    return null;
                string[] splitter = {"."};
                var memberNames = propertyPath.Split(splitter, StringSplitOptions.None);

                //MemberInfo[] memberInfos;
                var nextTargetValue = targetValue;
                MemberInfo memberInfo = null;
                //object memberValue = null;

                // ドットで区切られたメンバー名配列で深堀りしていく
                foreach (var memberName in memberNames)
                {
                    targetValue = nextTargetValue;
                    var memberInfos = targetType.GetMember(memberName);
                    if (memberInfos.Length == 0) return null;
                    memberInfo = memberInfos[0];
                    var memberType = memberInfo.GetReturnType();
                    var memberValue = memberInfo.GetMemberValue(targetValue);
                    // next
                    nextTargetValue = memberValue;
                    targetType = memberType;
                }

                object value; // セットする値
                var firstStrData = strData[0] as string;
                var firstStrDataLowerCase = firstStrData.ToLower();
                if (targetType == typeof(bool))
                {
                    value = firstStrDataLowerCase != "0" && firstStrDataLowerCase != "false" &&
                            firstStrDataLowerCase != "null";
                }
                else if (targetType == typeof(string))
                {
                    value = firstStrData;
                }
                else if (targetType == typeof(float))
                {
                    value = Single.Parse(firstStrData);
                }
                else if (targetType == typeof(double))
                {
                    value = Double.Parse(firstStrData);
                }
                else if (targetType == typeof(Vector3))
                {
                    if (strData.Count >= 3)
                    {
                        var x = Single.Parse(strData[0] as string);
                        var y = Single.Parse(strData[1] as string);
                        var z = Single.Parse(strData[2] as string);
                        value = new Vector3(x, y, z);
                    }
                    else
                    {
                        value = new Vector3(0, 0, 0);
                        Debug.LogError("Vector3を作成しようとしたが入力データが不足していた");
                    }
                }
                else
                {
                    // enum値などこちら
                    value = Int32.Parse(firstStrData);
                }

                memberInfo.SetMemberValue(targetValue, value);

                return value;
            }
            catch
            {
                throw;
            }
        }

        public static void SetupAddComponent(GameObject go, Dictionary<string, object> json)
        {
            if (json == null) return;
            var typeName = json.Get("type_name");
            if (typeName == null)
                return;
            var type = Type.GetType(typeName);
            if (type == null)
            {
                Debug.LogError($"Baum2 error*** Type.GetType({typeName})failed.");
                return;
            }

            var component = go.AddComponent(type);
        }
#endif
        /**
         * 
         */
        public static void SetupComponents(GameObject go, List<object> json)
        {
            /* フォーマットは以下のような感じでくる
             "components": [
              {
                "type": "Doozy.Engine.UI.UIButton, Doozy, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
                "name": "uibutton-",
                "method": "add",
                "properties": [
                  {
                    "path": "aaaa",
                    "values": ["bbbbb"]
                  },
                  {
                    "path": "aaaa",
                    "values": ["bbbbb"]
                  }
                ]
              }
            ]
             */
#if ODIN_INSPECTOR
            if (json == null) return;
            foreach (Dictionary<string, object> componentJson in json)
            {
                var typeName = componentJson.Get("type");
                if (typeName == null) continue;

                var componentType = Type.GetType(typeName);
                if (componentType == null)
                {
                    Debug.LogError($"Baum2 error*** Type.GetType({typeName})failed.");
                    return;
                }

                var component = go.AddComponent(componentType);

                var properties = componentJson.Get<List<object>>("properties");
                foreach (Dictionary<string, object> property in properties)
                {
                    SetProperty(componentType, component, property.Get("path"), property.Get<List<object>>("values"));
                }
            }
#endif
        }

        public static HorizontalOrVerticalLayoutGroup SetupLayoutGroupParam(GameObject go,
            Dictionary<string, object> layoutJson)
        {
            var method = "";
            if (layoutJson.ContainsKey("method"))
            {
                method = layoutJson.Get("method");
            }

            HorizontalOrVerticalLayoutGroup layoutGroup = null;

            if (method == "vertical")
            {
                var verticalLayoutGroup = go.AddComponent<VerticalLayoutGroup>();
                layoutGroup = verticalLayoutGroup;
            }

            if (method == "horizontal")
            {
                var horizontalLayoutGroup = go.AddComponent<HorizontalLayoutGroup>();
                layoutGroup = horizontalLayoutGroup;
            }

            if (layoutGroup == null)
            {
                return null;
            }

            // child control 子オブジェクトのサイズを変更する
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            if (layoutJson.ContainsKey("padding"))
            {
                var padding = layoutJson.GetDic("padding");
                var left = padding.GetInt("left");
                var right = padding.GetInt("right");
                var top = padding.GetInt("top");
                var bottom = padding.GetInt("bottom");
                if (left != null && right != null && top != null && bottom != null)
                {
                    var paddingRectOffset = new RectOffset(left.Value, right.Value, top.Value, bottom.Value);
                    layoutGroup.padding = paddingRectOffset;
                }
            }

            if (method == "horizontal")
            {
                var spacing = layoutJson.GetFloat("spacing_x");
                if (spacing != null) layoutGroup.spacing = spacing.Value;
            }

            if (method == "vertical")
            {
                var spacing = layoutJson.GetFloat("spacing_y");
                if (spacing != null) layoutGroup.spacing = spacing.Value;
            }

            var childAlignment = GetChildAlignment(layoutJson);
            if (childAlignment != null)
            {
                layoutGroup.childAlignment = childAlignment.Value;
            }

            var controlChildSize = layoutJson.Get("control_child_size");
            if (!String.IsNullOrEmpty(controlChildSize))
            {
                if (controlChildSize.Contains("width"))
                    layoutGroup.childControlWidth = true;
                if (controlChildSize.Contains("height"))
                    layoutGroup.childControlHeight = true;
            }

            var controlChildScale = layoutJson.Get("use_child_scale");
            if (!String.IsNullOrEmpty(controlChildScale))
            {
#if UNITY_2019_1_OR_NEWER
                if (controlChildScale.Contains("width"))
                    layoutGroup.childScaleWidth = true;
                if (controlChildScale.Contains("height"))
                    layoutGroup.childScaleHeight = true;
#endif
            }

            var childForceExpand = layoutJson.Get("child_force_expand");
            if (!String.IsNullOrEmpty(childForceExpand))
            {
                if (childForceExpand.Contains("width"))
                    layoutGroup.childForceExpandWidth = true;
                if (childForceExpand.Contains("height"))
                    layoutGroup.childForceExpandWidth = true;
            }

            return layoutGroup;
        }

        public static GridLayoutGroup SetupGridLayoutGroupParam(GameObject go,
            Dictionary<string, object> layoutJson)
        {
            if (layoutJson == null) return null;

            var layoutGroup = go.AddComponent<GridLayoutGroup>();

            if (layoutJson.ContainsKey("padding"))
            {
                var padding = layoutJson.GetDic("padding");
                var left = padding.GetInt("left");
                var right = padding.GetInt("right");
                var top = padding.GetInt("top");
                var bottom = padding.GetInt("bottom");
                var paddingRectOffset = new RectOffset(left.Value, right.Value, top.Value, bottom.Value);
                layoutGroup.padding = paddingRectOffset;
            }

            var spacingX = layoutJson.GetFloat("spacing_x");
            var spacingY = layoutJson.GetFloat("spacing_y");

            layoutGroup.spacing = new Vector2(spacingX.Value, spacingY.Value);

            var cellWidth = layoutJson.GetFloat("cell_max_width");
            var cellHeight = layoutJson.GetFloat("cell_max_height");
            layoutGroup.cellSize = new Vector2(cellWidth.Value, cellHeight.Value);

            var fixedRowCount = layoutJson.GetInt("fixed_row_count");
            if (fixedRowCount != null)
            {
                layoutGroup.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                layoutGroup.constraintCount = fixedRowCount.Value;
            }

            var fixedColumnCount = layoutJson.GetInt("fixed_column_count");
            if (fixedColumnCount != null)
            {
                layoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layoutGroup.constraintCount = fixedColumnCount.Value;
            }

            var childAlignment = GetChildAlignment(layoutJson);
            if (childAlignment != null)
            {
                layoutGroup.childAlignment = childAlignment.Value;
            }

            var startAxis = layoutJson.Get("start_axis");
            switch (startAxis)
            {
                case "vertical":
                    layoutGroup.startAxis = GridLayoutGroup.Axis.Vertical;
                    break;
                case "horizontal":
                    layoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
                    break;
            }

            // 左上から配置スタート
            layoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;

            return layoutGroup;
        }

        public static void SetupLayoutElement(GameObject go, Dictionary<string, object> layoutElement)
        {
            if (layoutElement == null) return;
            var componentLayoutElement = go.AddComponent<LayoutElement>();

            var minWidth = layoutElement.GetFloat("min_width");
            if (minWidth != null)
            {
                componentLayoutElement.minWidth = minWidth.Value;
            }

            var minHeight = layoutElement.GetFloat("min_height");
            if (minHeight != null)
            {
                componentLayoutElement.minHeight = minHeight.Value;
            }

            var preferredWidth = layoutElement.GetFloat("preferred_width");
            if (preferredWidth != null)
            {
                componentLayoutElement.preferredWidth = preferredWidth.Value;
            }

            var preferredHeight = layoutElement.GetFloat("preferred_height");
            if (preferredHeight != null)
            {
                componentLayoutElement.preferredHeight = preferredHeight.Value;
            }

            var ignoreLayout = layoutElement.GetBool("ignore_layout");
            if (ignoreLayout != null)
            {
                componentLayoutElement.ignoreLayout = ignoreLayout.Value;
            }
        }

        public static void SetupLayoutGroup(GameObject go, Dictionary<string, object> layout)
        {
            if (layout == null) return;

            if (layout["method"] is string)
            {
                var method = (layout["method"] as string).ToLower();
                switch (method)
                {
                    case "vertical":
                    case "horizontal":
                    {
                        var layoutGroup = SetupLayoutGroupParam(go, layout);
                        break;
                    }
                    case "grid":
                    {
                        var gridLayoutGroup = SetupGridLayoutGroupParam(go, layout);
                        break;
                    }
                    default:
                        break;
                }
            }
        }

        public static void SetupCanvasGroup(GameObject go, Dictionary<string, object> canvasGroup)
        {
            if (canvasGroup != null)
            {
                go.AddComponent<CanvasGroup>();
            }
        }

        public static void SetupRectMask2D(GameObject go, bool? param)
        {
            if (param != null && param.Value)
            {
                go.AddComponent<RectMask2D>(); // setupMask
            }
        }

        public static void SetupMask(GameObject go, Dictionary<string, object> param)
        {
            if (param != null)
            {
                var mask = go.AddComponent<Mask>(); // setupMask
                var showMaskGraphic = param.GetBool("show_mask_graphic");
                if (showMaskGraphic != null)
                {
                    mask.showMaskGraphic = showMaskGraphic.Value;
                }
            }
        }


        /// <summary>
        /// Scrollオブションの対応
        /// ViewportとContentを結びつける  
        /// </summary>
        /// <param name="goViewport"></param>
        /// <param name="goContent"></param>
        /// <param name="scrollRect"></param>
        public static void SetupScrollRect(GameObject goViewport, GameObject goContent,
            Dictionary<string, object> scrollRect)
        {
            if (scrollRect == null)
            {
                return;
            }

            var scrollRectComponent = goViewport.AddComponent<ScrollRect>();
            scrollRectComponent.content = goContent.GetComponent<RectTransform>(); // Content
            scrollRectComponent.viewport = goViewport.GetComponent<RectTransform>(); // 自分自身がViewportになる
            scrollRectComponent.vertical = false;
            scrollRectComponent.horizontal = false;

            bool? b;
            if ((b = scrollRect.GetBool("horizontal")) != null)
            {
                scrollRectComponent.horizontal = b.Value;
            }

            if ((b = scrollRect.GetBool("vertical")) != null)
            {
                scrollRectComponent.vertical = b.Value;
            }
        }

        public static void SetupRectTransform(GameObject root, Dictionary<string, object> rectTransformJson)
        {
            var rect = root.GetComponent<RectTransform>();

            // 先にPivotの設定をしてから Anchorの設定をする
            var pivot = rectTransformJson.GetDic("pivot").GetVector2("x", "y");
            if (pivot != null) rect.pivot = pivot.Value;

            var anchorMin = rectTransformJson.GetDic("anchor_min").GetVector2("x", "y");
            var anchorMax = rectTransformJson.GetDic("anchor_max").GetVector2("x", "y");
            var offsetMin = rectTransformJson.GetDic("offset_min").GetVector2("x", "y");
            var offsetMax = rectTransformJson.GetDic("offset_max").GetVector2("x", "y");
            if (anchorMin != null) rect.anchorMin = anchorMin.Value;
            if (anchorMax != null) rect.anchorMax = anchorMax.Value;
            if (offsetMin != null) rect.offsetMin = offsetMin.Value;
            if (offsetMax != null) rect.offsetMax = offsetMax.Value;
        }

        public static void SetLayer(GameObject go, string layerName)
        {
            switch (layerName)
            {
                case "Default":
                    go.layer = 0;
                    break;
                case "UI":
                    go.layer = 5;
                    break;
            }
        }
    }
}
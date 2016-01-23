using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Css;
using AngleSharp.Parser.Html;
using ICSharpCode.WpfDesign.XamlDom;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;

namespace Xaml2HtmlParser
{
    public static class Parser
    {
        public static string ParseXamlToHtml(string xaml, Assembly[] assemblies)
        {
            var settings = new XamlParserSettings();

            foreach (var assembly in assemblies)
            {
                settings.TypeFinder.RegisterAssembly(assembly);
            }
           
            var config = AngleSharp.Configuration.Default.WithCss(x => x.Options = new CssParserOptions() { IsIncludingUnknownDeclarations = true });

            using (var xmlReader = XmlReader.Create(new StringReader(xaml)))
            {
                var xamlObj = XamlParser.Parse(xmlReader, settings);
                var parser = new HtmlParser(config);
                var htmlDocument = parser.Parse("");
                ParseObject(xamlObj.RootElement, htmlDocument, htmlDocument.DocumentElement);

                return htmlDocument.DocumentElement.OuterHtml;
            }
        }

        private static IElement ParseObject(XamlPropertyValue @object, IHtmlDocument htmlDocument, IElement outerElement)
        {
            IElement element = null;

            if (@object is XamlObject)
            {
                bool alreadyAdded = false;
                bool childsParsed = false;

                var xamlObject = (XamlObject)@object;


                switch (xamlObject.ElementType.Name)
                {
                    case "Viewbox":
                        {
                            element = htmlDocument.CreateElement("div");
                            //todo: stretch, zoom??
                            break;
                        }
                    case "Border":
                        {
                            element = htmlDocument.CreateElement("div");
                            break;
                        }
                    case "Canvas":
                        {
                            element = htmlDocument.CreateElement("div");
                            ((IHtmlElement)element).Style.Position = "absolute";
                            break;
                        }
                    case "StackPanel":
                        {
                            element = htmlDocument.CreateElement("div");
                            ((IHtmlElement)element).Style.Display = "flex";
                            ((IHtmlElement)element).Style.FlexDirection = "column";
                            break;
                        }
                    case "WrapPanel":
                        {
                            element = htmlDocument.CreateElement("div");
                            ((IHtmlElement)element).Style.Display = "flex";
                            ((IHtmlElement)element).Style.FlexWrap = "wrap";
                            ((IHtmlElement)element).Style.FlexDirection = "column";
                            break;
                        }
                    case "DockPanel":
                        {
                            element = htmlDocument.CreateElement("div");
                            ((IHtmlElement)element).Style.Display = "flex";
                            ((IHtmlElement)element).Style.FlexDirection = "column";
                            break;
                        }
                    case "Grid":
                        {
                            var tbl = htmlDocument.CreateElement("table");
                            ((IHtmlElement)tbl).Style.Width = "100%";
                            ((IHtmlElement)tbl).Style.Height = "100%";
                            outerElement.AppendChild(tbl);
                            alreadyAdded = true;
                            childsParsed = true;

                            var grid = xamlObject.Instance as Grid;
                            foreach (var xamlProperty in xamlObject.Properties.Where(x => x.PropertyName != "Children"))
                            {
                                ParseProperty(xamlProperty, htmlDocument, (IHtmlElement)tbl);
                            }

                            var children = xamlObject.Properties.FirstOrDefault(x => x.PropertyName == "Children");

                            for (int n = 0; n < (grid.RowDefinitions.Count > 0 ? grid.RowDefinitions.Count : 1); n++)
                            {
                                var row = htmlDocument.CreateElement("tr");
                                ((IHtmlElement)row).Style.VerticalAlign = "top";
                                tbl.AppendChild(row);
                                if (grid.RowDefinitions.Count > 0)
                                {
                                    var rd = grid.RowDefinitions[n];
                                    ((IHtmlElement)row).Style.Height = ParseGridLenth(rd.Height);
                                }
                                row.ClassList.Add("visuGrid");

                                for (int p = 0; p < (grid.ColumnDefinitions.Count > 0 ? grid.ColumnDefinitions.Count : 1); p++)
                                {
                                    var td = htmlDocument.CreateElement("td");
                                    td.ClassList.Add("visuGrid");
                                    row.AppendChild(td);

                                    element = htmlDocument.CreateElement("div");
                                    td.AppendChild(element);

                                    ((IHtmlElement)element).Style.Width = "100%";
                                    ((IHtmlElement)element).Style.Height = "100%";

                                    if (grid.ColumnDefinitions.Count > 0)
                                    {
                                        var rd = grid.ColumnDefinitions[p];
                                        ((IHtmlElement)td).Style.Width = ParseGridLenth(rd.Width);
                                    }

                                    //Row Col Span should be used

                                    var p1 = p;
                                    var n1 = n;
                                    var childs = children.CollectionElements.OfType<XamlObject>().Where(x => Grid.GetColumn((UIElement)x.Instance) == p1 && Grid.GetRow((UIElement)x.Instance) == n1);
                                    foreach (var child in childs)
                                    {
                                        var el = ParseObject(child, htmlDocument, element);
                                        //((IHtmlElement) el).Style.Position = null;
                                    }
                                }
                            }
                            element = tbl;
                            break;
                        }
                    case "Image":
                        {
                            element = htmlDocument.CreateElement("div");
                            break;
                        }
                    case "Rectangle":
                        {
                            element = htmlDocument.CreateElement("div");
                            break;
                        }
                    case "Button":
                        {
                            element = htmlDocument.CreateElement("button");
                            break;
                        }
                    case "TextBlock":
                        {
                            element = htmlDocument.CreateElement("span");
                            break;
                        }
                    case "TextBox":
                        {
                            element = htmlDocument.CreateElement("input");
                            element.SetAttribute("type", "text");
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }

                if (element != null)
                {
                    if (xamlObject.ParentObject != null && (xamlObject.ParentObject.Instance is Grid || xamlObject.ParentObject.Instance is Canvas))
                    {
                        //((IHtmlElement) element).Style.Position = "absolute";
                    }

                    if (xamlObject.ParentObject != null && xamlObject.ParentObject.Instance is Grid)
                    {
                        if (((FrameworkElement)xamlObject.Instance).HorizontalAlignment != HorizontalAlignment.Stretch)
                        {
                            SetFixedWidth((IHtmlElement)element, xamlObject);
                        }
                        else
                        {
                            ((IHtmlElement)element).Style.Width = "100%";
                        }

                        if (((FrameworkElement)xamlObject.Instance).VerticalAlignment != VerticalAlignment.Stretch)
                        {
                            SetFixedHeight((IHtmlElement)element, xamlObject);
                        }
                        else
                        {
                            ((IHtmlElement)element).Style.Height = "100%";
                        }
                    }
                    else
                    {
                        SetFixedWidth((IHtmlElement)element, xamlObject);
                        SetFixedHeight((IHtmlElement)element, xamlObject);
                    }
                }

                if (element != null && !childsParsed)
                {
                    foreach (var xamlProperty in xamlObject.Properties)
                    {
                        ParseProperty(xamlProperty, htmlDocument, (IHtmlElement)element);
                    }

                    if (!alreadyAdded)
                    {
                        outerElement.AppendChild(element);
                    }
                }
            }
            else if (@object is XamlTextValue)
            {
                var text = @object as XamlTextValue;
                outerElement.TextContent = text.Text;
            }

            return element;
        }

        private static void ParseProperty(XamlProperty property, IHtmlDocument htmlDocument, IHtmlElement element)
        {
            if (property.IsCollection)
            {
                foreach (var prp in property.CollectionElements)
                {
                    if (prp is XamlObject)
                    {
                        if (((XamlObject)prp).Instance is FrameworkElement)
                        {
                            ParseObject((XamlObject)prp, htmlDocument, element);
                        }
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(property.ParentObject.ContentPropertyName) && property.PropertyName == property.ParentObject.ContentPropertyName)
                {
                    ParseObject(property.PropertyValue, htmlDocument, element);
                }
                else
                {
                    switch (property.PropertyName)
                    {
                        case "Row":
                            {
                                var rw = Convert.ToInt32(property.ValueOnInstance);
                                //element.Style.SetProperty("grid-row", (rw + 1).ToString());
                                return;
                            }
                        case "RowSpan":
                            {
                                //element.Style.SetProperty("grid-row-span", property.ValueOnInstance.ToString());
                                return;
                            }
                        case "Column":
                            {
                                var rw = Convert.ToInt32(property.ValueOnInstance);
                                //element.Style.SetProperty("grid-column", (rw + 1).ToString());
                                return;
                            }
                        case "ColumnSpan":
                            {
                                //element.Style.SetProperty("grid-column-span", property.ValueOnInstance.ToString());
                                return;
                            }
                        case "Orientation":
                            {
                                element.Style.FlexDirection = property.ValueOnInstance.ToString() == "Vertical" ? "Row" : "Column";
                                return;
                            }
                        case "Text":
                            {
                                //When ctrl is a TextBox / TranslateTextBlock
                                element.TextContent = (property.ValueOnInstance ?? "").ToString();
                                return;
                            }
                        case "Stroke":
                            {
                                //When ctrl is a Rectangle
                                element.Style.BorderColor = ParseXamlColor(property.ValueOnInstance);
                                element.Style.BorderStyle = "solid";
                                return;
                            }
                        case "StrokeThickness":
                            {
                                //When ctrl is a Rectangle
                                element.Style.BorderWidth = property.ValueOnInstance.ToString() + "px";
                                element.Style.BorderStyle = "solid";
                                return;
                            }
                        case "Width":
                            {
                                element.Style.Width = property.ValueOnInstance.ToString() + "px";
                                return;
                            }
                        case "Height":
                            {
                                element.Style.Height = property.ValueOnInstance.ToString() + "px";
                                return;
                            }
                        case "Background":
                        case "Fill":
                            {
                                element.Style.Background = ParseXamlColor(property.ValueOnInstance);
                                return;
                            }
                        case "Margin":
                            {
                                element.Style.Left = ((Thickness)property.ValueOnInstance).Left.ToString() + "px";
                                element.Style.Top = ((Thickness)property.ValueOnInstance).Top.ToString() + "px";
                                return;
                            }
                        case "Left":
                            {
                                //if (property.ParentObject.ParentObject.Instance is Canvas)
                                //element.Style.Position = "absolute";
                                element.Style.Left = property.ValueOnInstance.ToString() + "px";
                                return;
                            }
                        case "Top":
                            {
                                //element.Style.Position = "absolute";
                                element.Style.Top = property.ValueOnInstance.ToString() + "px";
                                return;
                            }
                        case "FontSize":
                            {
                                element.Style.FontSize = property.ValueOnInstance.ToString() + "pt";
                                return;
                            }
                        case "FontWeight":
                            {
                                element.Style.FontWeight = property.ValueOnInstance.ToString();
                                return;
                            }
                        case "RenderTransform":
                            {
                                ParseTransform(element, property.ValueOnInstance as Transform);
                                return;
                            }
                        case "Opacity":
                            {
                                element.Style.Opacity = property.ValueOnInstance.ToString();
                                return;
                            }
                        case "Ignorable":
                            {
                                return;
                            }
                        default:
                            {
                                if (property.ValueOnInstance != null)
                                {
                                    var nm = property.PropertyName[0].ToString().ToLower() + property.PropertyName.Substring(1);
                                    nm = Regex.Replace(nm, @"(?<!_)([A-Z])", "-$1");
                                    element.SetAttribute(nm, property.ValueOnInstance.ToString());
                                }
                                return;
                            }
                    }
                }
            }
        }

        private static string ParseGridLenth(GridLength value)
        {
            if (value.IsAuto)
                return "auto";

            var unit = "px";
            if (value.IsStar)
            {
                unit = "%"; //maybe fr
                return (value.Value * 100).ToString() + unit;
            }

            return value.Value.ToString() + unit;
        }

        private static void ParseTransform(IHtmlElement element, Transform value)
        {
            if (value is RotateTransform)
            {
                element.Style.Transform = "rotate(" + ((RotateTransform)value).Angle + "deg)";
            }
        }

        private static void SetFixedWidth(IHtmlElement element, XamlObject @object)
        {
            if (((FrameworkElement)@object.Instance).Width > 0)
                ((IHtmlElement)element).Style.Width = ((FrameworkElement)@object.Instance).Width + "px";
        }

        private static void SetFixedHeight(IHtmlElement element, XamlObject @object)
        {
            if (((FrameworkElement)@object.Instance).Height > 0)
                ((IHtmlElement)element).Style.Height = ((FrameworkElement)@object.Instance).Height + "px";
        }

        private static string ParseXamlColor(object value)
        {
            if (value is SolidColorBrush)
            {
                var scb = (SolidColorBrush)value;

                if (scb.Color.A == 255)
                {
                    return "rgb(" + scb.Color.R + "," + scb.Color.G + "," + scb.Color.B + ")";
                }

                return "rgba(" + scb.Color.R + "," + scb.Color.G + "," + scb.Color.B + "," + (255.0 / scb.Color.A) + ")";
            }

            return null;
        }
    }
}

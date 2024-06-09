using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Documenation.Core;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Documenation.Generator
{
    public class DocumentationWindow : OdinEditorWindow
    {
        private Vector2 scrollPosButtons;

        private Documentation documentation = new Documentation();

        private List<TagButton> buttons = new List<TagButton>(4);

        private Color defaultColor;

        [ShowInInspector, Space(10)]
        [InlineEditor(InlineEditorObjectFieldModes.Hidden)]
        [ListDrawerSettings(HideAddButton = true, HideRemoveButton = true, DraggableItems = false, IsReadOnly = true, ShowFoldout = false, ShowPaging = false)]
        private List<DocumentationView> common = new List<DocumentationView>();

        [MenuItem("Tools/Documentation/DocumentationWindow")]
        public static void ShowDocumentationWindow()
        {
            GetWindow<DocumentationWindow>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            defaultColor = GUI.backgroundColor;

            foreach (var doc in documentation.Documentations)
            {
                foreach (var TagName in doc.SegmentTypes)
                    buttons.Add(new TagButton { Name = TagName });
            }

            buttons = buttons.OrderBy(x => x.Name).Distinct().ToList();
            AddDefaultComments();
        }

        protected override void OnImGUI()
        {
            GUILayout.BeginHorizontal();
            scrollPosButtons = EditorGUILayout.BeginScrollView(scrollPosButtons, GUILayout.MaxWidth(400f), GUILayout.Width(150f), GUILayout.MinWidth(60f));
            DrawButtons();
            GUILayout.EndScrollView();
            GUI.backgroundColor = defaultColor;
            base.OnImGUI();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUI.backgroundColor = defaultColor;
            if (GUILayout.Button("Reset", GUILayout.Height(30f)))
            {
                for (int i = 0; i < buttons.Count; i++)
                {
                    var data = buttons[i];
                    data.IsActve = false;
                    buttons[i] = data;
                    RedrawData();
                }
            }

            if (GUILayout.Button("Search", GUILayout.Height(30f)))
            {
                GetWindow<DocumentationSearchWinow>();
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void RedrawData()
        {
            foreach (var view in common)
                DestroyImmediate(view);

            common.Clear();


            var tags = buttons.Where(x => x.IsActve).ToArray();

            var neededDocs = new List<DocumentationRepresentation>(16);

            foreach (var d in documentation.Documentations)
            {
                foreach (var tag in tags)
                {
                    if (d.SegmentTypes.Contains(tag.Name))
                    {
                        neededDocs.Add(d);
                        continue;
                    }
                    else
                    {
                        neededDocs.Remove(d);
                        break;
                    }
                }
            }

            foreach (var needed in neededDocs)
            {
                var view = CreateInstance<DocumentationView>().Init(needed);

                if (!common.Contains(view))
                    common.Add(view);
            }

            AddDefaultComments();
        }

        private void AddDefaultComments()
        {
            if (common.Count > 0)
                return;

            common.Add(CreateInstance<DocumentationView>().Init(new DocumentationRepresentation
            {
                Comments = new string[]
                    { "Select tag from left menu to have a look on objects marked by this tag",
                      "How to use Documentation feature  - u should add attribute [Documentation] to your object",
                      "This attribute have arguments  = (string tag, params string[] tagsAndComment)",
                      "all parametr except last one, will be tags, and last one will be comment for this object",
                      $"You can use simple strings like [Documentation({CParse.Quote}Ability{CParse.Quote}, {CParse.Quote}This is parent class for all abilities{CParse.Quote})]",
                      "Or use Doc (static class helper) like [Documentation({CParse.Quote}Doc.Ability{CParse.Quote}, {CParse.Quote}This is parent class for all abilities{CParse.Quote})]",
                      "Doc is partial class whom helds const for fast and consisten using of tags, better will be use this class for all tags",
                      "Better will have additional part of this class with tags whom depends from current project"
                    },
                DataType = "WELCOME TO DOCUMENTATION",
            }));
        }

        private void DrawButtons()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i].IsActve)
                    GUI.backgroundColor = Color.cyan;
                else
                    GUI.backgroundColor = defaultColor;

                if (GUILayout.Button(buttons[i].Name))
                {
                    var data = buttons[i];
                    data.IsActve = !buttons[i].IsActve;
                    buttons[i] = data;
                    RedrawData();
                }
            }
        }

        [HideLabel]
        public class DocumentationView : ScriptableObject, IEquatable<DocumentationView>
        {
            [CustomValueDrawer(nameof(DrawLabelAsBox))]
            [InlineButton(nameof(Open))]
            public string Name;

            [TextArea(3, 25), ReadOnly]
            public string Comments;

            private string GroupName => Name.Replace("Component", "").Replace("BluePrint", "");

            [NonSerialized] private string fullName;

            public DocumentationView Init(DocumentationRepresentation documentationRepresentation)
            {
                Name = documentationRepresentation.DataType;
                fullName = documentationRepresentation.DataType;

                foreach (var c in documentationRepresentation.Comments)
                    Comments += c + CParse.Dot + "\n";

                return this;
            }

            private string DrawLabelAsBox()
            {
                Sirenix.Utilities.Editor.SirenixEditorGUI.BeginBox(GroupName);
                Sirenix.Utilities.Editor.SirenixEditorGUI.EndBox();
                return GroupName;
            }

            private void Open()
            {
                DirectoryInfo lookingFor = new DirectoryInfo(Application.dataPath);
                var find = lookingFor.GetFiles(fullName + ".cs", SearchOption.AllDirectories);

                if (find != null && find.Length > 0)
                    UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(find[0].FullName, 0, 0);
            }

            public bool Equals(DocumentationView other)
            {
                return Name.Equals(other.Name) && Comments.Equals(other.Comments);
            }
        }

        public struct TagButton
        {
            public string Name;
            public bool IsActve;

            public override bool Equals(object obj)
            {
                return obj is TagButton button &&
                       Name == button.Name;
            }

            public override int GetHashCode()
            {
                return 539060726 + EqualityComparer<string>.Default.GetHashCode(Name);
            }
        }
    }
}
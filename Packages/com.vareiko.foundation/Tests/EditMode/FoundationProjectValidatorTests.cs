using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Vareiko.Foundation.Editor.Validation;
using Vareiko.Foundation.UI;

namespace Vareiko.Foundation.EditorTests
{
    public sealed class FoundationProjectValidatorTests
    {
        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void RunValidation_ReturnsReportWithoutDialog()
        {
            FoundationProjectValidator.ValidationReport report = RunOpenSceneValidation();

            Assert.That(report, Is.Not.Null);
            Assert.That(report.Issues.Count, Is.GreaterThan(0));
        }

        [Test]
        public void RunValidation_ReportsDuplicateUiIds()
        {
            GameObject root = new GameObject("UIRoot");
            root.AddComponent<UIRegistry>();
            CreateElement<UIElement>(root.transform, "First", "dup.id");
            CreateElement<UIElement>(root.transform, "Second", "dup.id");

            FoundationProjectValidator.ValidationReport report = RunOpenSceneValidation();

            AssertHasIssue(report, FoundationProjectValidator.ValidationSeverity.Error, "UI-011");
        }

        [Test]
        public void RunValidation_ReportsEmptyScreenAndWindowIds()
        {
            GameObject root = new GameObject("UIRoot");
            root.AddComponent<UIRegistry>();
            CreateElement<UIScreen>(root.transform, "Screen", string.Empty);
            CreateElement<UIWindow>(root.transform, "Window", " ");

            FoundationProjectValidator.ValidationReport report = RunOpenSceneValidation();

            Assert.That(report.Issues.Count(issue => issue.Code == "UI-010" && issue.Severity == FoundationProjectValidator.ValidationSeverity.Error), Is.EqualTo(2));
        }

        [Test]
        public void RunValidation_ReportsCollectionTemplateAndLayoutWarnings()
        {
            GameObject root = new GameObject("CollectionRoot");
            root.AddComponent<UIRegistry>();
            GameObject container = new GameObject("Container");
            container.transform.SetParent(root.transform, false);
            container.AddComponent<HorizontalLayoutGroup>();
            container.AddComponent<ContentSizeFitter>();

            GameObject templateGo = new GameObject("Template");
            templateGo.transform.SetParent(container.transform, false);
            UIItemView template = templateGo.AddComponent<UIItemView>();
            SetPrivateField(template, "_id", "template.id");

            UIItemCollectionBinder collection = root.AddComponent<UIItemCollectionBinder>();
            SetPrivateField(collection, "_itemPrefab", template);
            SetPrivateField(collection, "_container", container.transform);

            FoundationProjectValidator.ValidationReport report = RunOpenSceneValidation();

            AssertHasIssue(report, FoundationProjectValidator.ValidationSeverity.Error, "UI-020");
            AssertHasIssue(report, FoundationProjectValidator.ValidationSeverity.Error, "UI-021");
            AssertHasIssue(report, FoundationProjectValidator.ValidationSeverity.Warning, "UI-030");
            AssertHasIssue(report, FoundationProjectValidator.ValidationSeverity.Warning, "UI-031");
        }

        [Test]
        public void RunValidation_ReportsSuspiciousRaycastTargets()
        {
            GameObject root = new GameObject("GraphicRoot");
            root.AddComponent<UIRegistry>();
            CreateElement<UIElement>(root.transform, "Aux", string.Empty);
            GameObject imageGo = new GameObject("DecorativeImage");
            imageGo.transform.SetParent(root.transform, false);
            Image image = imageGo.AddComponent<Image>();
            image.raycastTarget = true;

            FoundationProjectValidator.ValidationReport report = RunOpenSceneValidation();

            AssertHasIssue(report, FoundationProjectValidator.ValidationSeverity.Warning, "UI-040");
        }

        private static FoundationProjectValidator.ValidationReport RunOpenSceneValidation()
        {
            return FoundationProjectValidator.RunValidation(new FoundationProjectValidator.ValidationOptions
            {
                ValidateReleaseGate = false,
                ValidateProjectContextPrefab = false,
                ValidateScenes = true,
                UseOpenScenesOnly = true
            });
        }

        private static T CreateElement<T>(Transform parent, string name, string id)
            where T : UIElement
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            T element = gameObject.AddComponent<T>();
            SetPrivateField(element, "_id", id);
            return element;
        }

        private static void AssertHasIssue(
            FoundationProjectValidator.ValidationReport report,
            FoundationProjectValidator.ValidationSeverity severity,
            string code)
        {
            Assert.That(report.Issues.Any(issue => issue.Severity == severity && issue.Code == code), Is.True, $"Expected {severity} {code}.");
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            FieldInfo field = null;
            System.Type type = instance.GetType();
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }

            Assert.That(field, Is.Not.Null, $"Missing field {fieldName} on {instance.GetType().Name}.");
            field.SetValue(instance, value);
        }
    }
}

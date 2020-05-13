﻿using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Rubberduck.Parsing.Annotations;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Refactorings.AnnotateDeclaration;
using Rubberduck.UI.Refactorings.AnnotateDeclaration;
using Rubberduck.VBEditor.SafeComWrappers;
using RubberduckTests.Mocks;

namespace RubberduckTests.Refactoring.AnnotateDeclaration
{
    [TestFixture]
    public class AnnotateDeclarationViewModelTests
    {
        [Test]
        [TestCase(DeclarationType.Module, 4, "Exposed")]
        [TestCase(DeclarationType.Procedure, 5, "DefaultMember")]
        [TestCase(DeclarationType.Variable, 3, "VariableDescription")]
        public void ApplicableAnnotationsAreFilteredBasedOnDeclarationType(
            DeclarationType declarationType,
            int expectedNumberOfAnnotations, 
            string expectedContainedAnnotation)
        {
            var viewModel = TestViewModel(declarationType);
            var applicableAnnotationNames = viewModel.ApplicableAnnotations
                .Select(annotation => annotation.Name)
                .ToList();

            Assert.AreEqual(expectedNumberOfAnnotations, applicableAnnotationNames.Count);
            Assert.Contains(expectedContainedAnnotation, applicableAnnotationNames);
        }

        [Test]
        public void AnnotationAlreadyPresent_DoesNotAllowMultiple_NotInApplicableAnnotations()
        {
            var viewModel = TestViewModel(DeclarationType.Function);
            var applicableAnnotationNames = viewModel.ApplicableAnnotations
                .Select(annotation => annotation.Name)
                .ToList();

            Assert.False(applicableAnnotationNames.Contains("DefaultMember"));
        }

        [Test]
        public void AnnotationAlreadyPresent_AllowsMultiple_InApplicableAnnotations()
        {
            var viewModel = TestViewModel(DeclarationType.Procedure);
            var applicableAnnotationNames = viewModel.ApplicableAnnotations
                .Select(annotation => annotation.Name)
                .ToList();

            Assert.True(applicableAnnotationNames.Contains("Ignore"));
        }
        
        [Test]
        public void AnnotationNull_Invalid()
        {
            var viewModel = TestViewModel(DeclarationType.Procedure);
            viewModel.Annotation = null;

            Assert.IsFalse(viewModel.IsValidAnnotation);
        }

        [Test]
        public void AnnotationNotNull_WithoutRequiredArguments_Valid()
        {
            var viewModel = TestViewModel(DeclarationType.Procedure);
            viewModel.Annotation = new TestMethodAnnotation();

            Assert.IsTrue(viewModel.IsValidAnnotation);
        }

        [Test]
        public void AnnotationNotNull_WithArgumentsWithError_Invalid()
        {
            var mockArgumentFactory = MockArgumentFactory(new List<bool> { true });
            
            var viewModel = TestViewModel(DeclarationType.Procedure, mockArgumentFactory.Object);
            viewModel.Annotation = new TestMethodAnnotation();
            viewModel.AddAnnotationArgument.Execute(null);

            Assert.IsFalse(viewModel.IsValidAnnotation);
        }

        [Test]
        public void AnnotationNotNull_WithArgumentsWithoutError_Valid()
        {
            var mockArgumentFactory = MockArgumentFactory(new List<bool> { false });

            var viewModel = TestViewModel(DeclarationType.Procedure, mockArgumentFactory.Object);
            viewModel.Annotation = new TestMethodAnnotation();
            viewModel.AddAnnotationArgument.Execute(null);

            Assert.IsTrue(viewModel.IsValidAnnotation);
        }

        [Test]
        public void AddArgumentAddsEmptyArgumentOfAppropriateType()
        {
            var mockArgumentFactory = MockArgumentFactory(new List<bool> { false });

            var viewModel = TestViewModel(DeclarationType.Procedure, mockArgumentFactory.Object);
            var initialArgumentCount = viewModel.AnnotationArguments.Count;
            viewModel.Annotation = new TestMethodAnnotation();
            viewModel.AddAnnotationArgument.Execute(null);

            var argumentsAdded = viewModel.AnnotationArguments.Count - initialArgumentCount;

            Assert.AreEqual(1, argumentsAdded);
            mockArgumentFactory.Verify(m => m.Create(AnnotationArgumentType.Text, string.Empty), Times.Once);
        }

        [Test]
        public void SetAnnotation_AddsRequiredArguments()
        {
            var viewModel = TestViewModel(DeclarationType.Procedure);
            viewModel.Annotation = new DescriptionAnnotation();

            Assert.AreEqual(1, viewModel.AnnotationArguments.Count);
        }

        [Test]
        public void SetAnnotation_NoRequiredArguments_AddsNone()
        {
            var viewModel = TestViewModel(DeclarationType.Procedure);
            viewModel.Annotation = new TestMethodAnnotation();

            Assert.IsFalse(viewModel.AnnotationArguments.Any());
        }

        [Test]
        public void CanAddArguments_NoArguments_False()
        {
            var viewModel = TestViewModel(DeclarationType.Procedure);
            viewModel.Annotation = new DefaultMemberAnnotation();
            var canAddArgument = viewModel.AddAnnotationArgument.CanExecute(null);

            Assert.IsFalse(canAddArgument);
        }

        [Test]
        public void CanAddArguments_NoOptionalArguments_False()
        {
            var viewModel = TestViewModel(DeclarationType.Procedure);
            viewModel.Annotation = new DescriptionAnnotation();
            var canAddArgument = viewModel.AddAnnotationArgument.CanExecute(null);

            Assert.IsFalse(canAddArgument);
        }

        [Test]
        public void CanAddArguments_OptionalArgumentsNotThere_True()
        {
            var viewModel = TestViewModel(DeclarationType.Procedure);
            viewModel.Annotation = new TestMethodAnnotation();
            var canAddArgument = viewModel.AddAnnotationArgument.CanExecute(null);

            Assert.IsTrue(canAddArgument);
        }

        [Test]
        public void CanAddArguments_MaxArgumentsSpecified_False()
        {
            var viewModel = TestViewModel(DeclarationType.Procedure);
            viewModel.Annotation = new TestMethodAnnotation();
            var addArgumentCommand = viewModel.AddAnnotationArgument;
            addArgumentCommand.Execute(null);
            var canAddArgument = addArgumentCommand.CanExecute(null);

            Assert.IsFalse(canAddArgument);
        }

        [Test]
        public void CanRemoveArguments_OptionalArgumentsPresent_True()
        {
            var viewModel = TestViewModel(DeclarationType.Procedure);
            viewModel.Annotation = new MemberAttributeAnnotation();
            var addArgumentCommand = viewModel.AddAnnotationArgument;
            addArgumentCommand.Execute(null);
            var canRemoveArgument = viewModel.RemoveAnnotationArgument.CanExecute(null);

            Assert.IsTrue(canRemoveArgument);
        }

        [Test]
        public void CanRemoveArguments_NoOptionalArgumentsPresent_False()
        {
            var viewModel = TestViewModel(DeclarationType.Procedure);
            viewModel.Annotation = new MemberAttributeAnnotation();

            var canRemoveArgument = viewModel.RemoveAnnotationArgument.CanExecute(null);

            Assert.IsFalse(canRemoveArgument);
        }

        [Test]
        public void RemoveArgument_LastArgumentRemoved()
        {
            var viewModel = TestViewModel(DeclarationType.Procedure);
            viewModel.Annotation = new IgnoreAnnotation();
            var addArgumentCommand = viewModel.AddAnnotationArgument;
            addArgumentCommand.Execute(null);
            addArgumentCommand.Execute(null);

            var initialArguments = viewModel.AnnotationArguments.ToList();
            viewModel.RemoveAnnotationArgument.Execute(null);

            var arguments = viewModel.AnnotationArguments.ToList();

            Assert.AreEqual(initialArguments.Count - 1, arguments.Count);
            for (var argumentIndex = 0; argumentIndex < arguments.Count; argumentIndex++)
            {
                Assert.AreSame(initialArguments[argumentIndex], arguments[argumentIndex]);
            }
        }

        [Test]
        public void SetAnnotation_ResetsArguments()
        {
            var viewModel = TestViewModel(DeclarationType.Procedure);
            viewModel.Annotation = new DescriptionAnnotation();
            viewModel.Annotation = new TestMethodAnnotation();

            Assert.IsFalse(viewModel.AnnotationArguments.Any());
        }

        [Test]
        public void SetAnnotation_SetsAnnotationOnModel()
        {
            var viewModel = TestViewModel(DeclarationType.Procedure);
            var annotation = new DescriptionAnnotation();
            viewModel.Annotation = annotation;
            
            Assert.AreSame(viewModel.Model.Annotation, annotation);
        }

        [Test]
        public void ModelIsInputModelFromCreation()
        {
            var target = TestDeclaration(DeclarationType.Procedure);
            var model = new AnnotateDeclarationModel(target, null);
            var viewModel = new AnnotateDeclarationViewModel(model, _testAnnotations, null);

            Assert.AreSame(model, viewModel.Model);
        }

        [Test]
        public void DialogOK_SetsArguments()
        {
            var viewModel = TestViewModel(DeclarationType.Procedure);
            viewModel.Annotation = new IgnoreAnnotation();
            var addArgumentCommand = viewModel.AddAnnotationArgument;
            addArgumentCommand.Execute(null);
            addArgumentCommand.Execute(null);

            var viewModelArguments = viewModel.AnnotationArguments.Select(argumentViewModel => argumentViewModel.Model).ToList();
            viewModel.OkButtonCommand.Execute(null);

            var modelArguments = viewModel.Model.Arguments;

            Assert.AreEqual(viewModelArguments.Count, modelArguments.Count);
        }


        private AnnotateDeclarationViewModel TestViewModel(DeclarationType targetDeclarationType, IAnnotation initialAnnotation = null)
        {
            var argumentFactory = MockArgumentFactory().Object;
            return TestViewModel(targetDeclarationType, argumentFactory, initialAnnotation);
        }

        private Mock<IAnnotationArgumentViewModelFactory> MockArgumentFactory(IReadOnlyList<bool> hasErrorSpecifications = null)
        {
            var hasErrorSpecs = hasErrorSpecifications ?? new List<bool>();
            var argumentCount = 0;

            var argumentFactory = new Mock<IAnnotationArgumentViewModelFactory>();
            argumentFactory.Setup(m => m.Create(It.IsAny<AnnotationArgumentType>(), It.IsAny<string>()))
                .Returns((AnnotationArgumentType argumentType, string argument) =>
                {
                    var hasError = argumentCount < hasErrorSpecs.Count && hasErrorSpecs[argumentCount];
                    return MockArgument(argumentType, argument, hasError).Object;
                });

            return argumentFactory;
        }

        private Mock<IAnnotationArgumentViewModel> MockArgument(AnnotationArgumentType argumentType, string argumentValue = null, bool hasError = false)
        {
            var mockArgument = new Mock<IAnnotationArgumentViewModel>();

            mockArgument.SetupGet(m => m.HasErrors).Returns(hasError);
            mockArgument.SetupGet(m => m.Model).Returns(new TypedAnnotationArgument(argumentType, argumentValue));

            return mockArgument;
        }

        private AnnotateDeclarationViewModel TestViewModel(DeclarationType targetDeclarationType, IAnnotationArgumentViewModelFactory argumentFactory, IAnnotation initialAnnotation = null)
        {
            var targetDeclaration = TestDeclaration(targetDeclarationType);
            var model = new AnnotateDeclarationModel(targetDeclaration, initialAnnotation);
            return new AnnotateDeclarationViewModel(model, _testAnnotations, argumentFactory);
        }

        private Declaration TestDeclaration(DeclarationType targetDeclarationType)
        {
            const string code = @"
Public myVar As Variant

'@Ignore MissingMemberAnnotationInspection
Public Sub Foo
End Sub

'@DefaultMember
Public Function Bar
End Function
";
            var vbe = MockVbeBuilder.BuildFromSingleModule(code, ComponentType.ClassModule, out _).Object;
            using (var state = MockParser.CreateAndParse(vbe))
            {
                return state.DeclarationFinder.UserDeclarations(targetDeclarationType).Single();
            }
        }

        private readonly IAnnotation[] _testAnnotations =
        {
            new IgnoreAnnotation(),
            new IgnoreModuleAnnotation(), 
            new TestMethodAnnotation(), 
            new TestModuleAnnotation(), 
            new DefaultMemberAnnotation(), 
            new ExposedModuleAnnotation(),
            new VariableDescriptionAnnotation(),
            new DescriptionAnnotation(), 
            new MemberAttributeAnnotation(),
            new ModuleDescriptionAnnotation(),
        };
    }
}
using System.Reflection;
using AutoFixture;
using Bunit;
using Fluxor;
using Mediator;
using Messentra.Features.Explorer.Resources;
using Messentra.Features.Jobs;
using Messentra.Infrastructure;
using Messentra.Infrastructure.AutoUpdater;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;

namespace Messentra.ComponentTests;

public class ComponentTestBase : BunitContext
{
    protected virtual bool RenderMudProviders => true;
    protected Fixture Fixture { get; } = new();
    protected Mock<IDispatcher> MockDispatcher { get; } = new();
    protected Mock<IMediator> MockMediator { get; } = new();
    protected IRenderedComponent<MudPopoverProvider> MudPopover { get; }
    protected IRenderedComponent<MudDialogProvider> MudDialog { get; }

    protected ComponentTestBase()
    {
        Services.AddMudServices();
        Services.AddSingleton(MockDispatcher.Object);
        Services.AddSingleton(Mock.Of<IActionSubscriber>());
        Services.AddSingleton(Mock.Of<IStore>());
        Services.AddSingleton(MockMediator.Object);
        Services.AddSingleton<FakeAutoUpdaterService>();
        Services.AddSingleton<IAutoUpdaterService>(sp => sp.GetRequiredService<FakeAutoUpdaterService>());
        Services.AddSingleton(Mock.Of<IFileSystem>());
        Services.AddSingleton(Mock.Of<IJobProgressNotifier>());
        JSInterop.Mode = JSRuntimeMode.Loose;
        RegisterStateTypes();
        RegisterResourceSelector();

        if (!RenderMudProviders) 
            return;
        
        MudPopover = base.Render<MudPopoverProvider>();
        MudDialog = base.Render<MudDialogProvider>();
    }
    
    protected IRenderedComponent<TComponent> RenderDialog<TComponent>(
        Action<DialogParameters>? parameterBuilder = null)
        where TComponent : IComponent =>
        RenderDialog<TComponent>(parameterBuilder ?? (_ => { }) , out _);
    
    protected IRenderedComponent<TComponent> RenderDialog<TComponent>(
        out IDialogReference dialogReference)
        where TComponent : IComponent =>
        RenderDialog<TComponent>(_ => { }, out dialogReference);

    protected IRenderedComponent<TComponent> RenderDialog<TComponent>(
        Action<DialogParameters> parameterBuilder,
        out IDialogReference dialogReference)
        where TComponent : IComponent
    {
        var dialogParameters = new DialogParameters();
        parameterBuilder.Invoke(dialogParameters);
        var dialogService = Services.GetRequiredService<IDialogService>();
        dialogReference = dialogService.ShowAsync<TComponent>("Test Dialog", dialogParameters).GetAwaiter().GetResult();

        return MudDialog.FindComponent<TComponent>();
    }

    protected TestState<T> GetState<T>() => Services.GetRequiredService<IState<T>>() as TestState<T> ??
                                            throw new InvalidOperationException();
    
    private void RegisterStateTypes()
    {
        var stateTypes = GetStateTypes();
        var stateInterface = typeof(IState<>);
        var testStateType = typeof(TestState<>);

        foreach (var stateType in stateTypes)
        {
            var genericTestStateType = testStateType.MakeGenericType(stateType);
            var defaultState = CreateDefaultInstance(stateType);
            var testState = Activator.CreateInstance(genericTestStateType, defaultState);
            var interfaceType = stateInterface.MakeGenericType(stateType);

            Services.AddSingleton(interfaceType, testState!);
        }
    }

    private static object CreateDefaultInstance(Type stateType)
    {
        // Try non-public parameterless constructor first ([FeatureState] convention)
        try
        {
            var instance = Activator.CreateInstance(stateType, BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
            if (instance != null) return instance;
        }
        catch (MissingMethodException) { }

        // Fall back: use the first public constructor, passing default values for each parameter
        var ctor = stateType.GetConstructors().FirstOrDefault();
        if (ctor != null)
        {
            var args = ctor.GetParameters()
                .Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null)
                .ToArray();
            return Activator.CreateInstance(stateType, args)!;
        }

        return Activator.CreateInstance(stateType)!;
    }

    private static Type[] GetStateTypes()
    {
        var types = typeof(Program).Assembly.GetTypes();

        // [FeatureState]-decorated types — the type itself is the state
        var fromAttribute = types
            .Where(t => t.GetCustomAttributes(typeof(FeatureStateAttribute), false).Length > 0)
            .ToList();

        // Custom Feature<T> subclasses — extract T as the state type
        var fromCustomFeature = types
            .Where(t => !t.IsAbstract)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFeature<>))
                .Select(i => i.GetGenericArguments()[0]))
            .Except(fromAttribute);

        return fromAttribute.Concat(fromCustomFeature).Distinct().ToArray();
    }

    private void RegisterResourceSelector()
    {
        var mockFeature = new Mock<IFeature<ResourceState>>();
        mockFeature.Setup(f => f.State).Returns(new ResourceState([], null, []));
        Services.AddScoped<ResourceSelector>(_ => new ResourceSelector(mockFeature.Object));
    }
}
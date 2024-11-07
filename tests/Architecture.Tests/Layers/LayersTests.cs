using System.Reflection;
using NetArchTest.Rules;

namespace Architecture.Tests.Layers;

public class LayersTests
{
    private static readonly Assembly _applicationAssembly = typeof(Agent.Application.DependencyInjection).Assembly;
    private static readonly Assembly _domainAssembly = typeof(Agent.Core.None).Assembly;
    private static readonly Assembly _infrastructureAssembly = typeof(Agent.Infrastructure.Extensions.ServiceExtensions).Assembly;
    private static readonly Assembly _presentationAssembly = typeof(Agent.API.Program).Assembly;

    [Fact]
    public void Application_ShouldNotHaveDependencyOn_Infrastructure()
    {
        TestResult result = Types.InAssembly(_applicationAssembly)
            .Should()
            .NotHaveDependencyOn(_infrastructureAssembly.GetName().Name)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Application_ShouldNotHaveDependencyOn_Presentation()
    {
        TestResult result = Types.InAssembly(_applicationAssembly)
            .Should()
            .NotHaveDependencyOn(_presentationAssembly.GetName().Name)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Domain_Should_NotHaveDependencyOnApplication()
    {
        TestResult result = Types.InAssembly(_domainAssembly)
            .Should()
            .NotHaveDependencyOn(_applicationAssembly.GetName().Name)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Domain_ShouldNotHaveDependencyOn_Infrastructure()
    {
        TestResult result = Types.InAssembly(_domainAssembly)
            .Should()
            .NotHaveDependencyOn(_infrastructureAssembly.GetName().Name)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Domain_ShouldNotHaveDependencyOn_Presentation()
    {
        TestResult result = Types.InAssembly(_domainAssembly)
            .Should()
            .NotHaveDependencyOn(_presentationAssembly.GetName().Name)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Infrastructure_ShouldNotHaveDependencyOn_Presentation()
    {
        TestResult result = Types.InAssembly(_infrastructureAssembly)
            .Should()
            .NotHaveDependencyOn(_presentationAssembly.GetName().Name)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
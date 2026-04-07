using Xunit;

namespace applanch.Tests.TestSupport;

/// <summary>
/// xUnit collection that serializes all WPF tests with respect to each other.
/// WPF tests share <see cref="System.Windows.Application.Current"/> and the WPF BAML
/// resource-loading infrastructure, which is not thread-safe across concurrent STA threads.
/// Grouping them into a single collection ensures they run one at a time while
/// non-WPF tests continue to run in parallel.
/// </summary>
[CollectionDefinition("WpfTests")]
public sealed class WpfTestCollection;

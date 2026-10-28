using Xunit;

// Core exposes its engine seams as statics (speech, announcement wording, nav input), and several
// tests install and restore them; parallel test classes would race on those, so run serially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

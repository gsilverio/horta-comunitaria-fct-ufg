using Xunit;

// Cada fixture administra bancos isolados e um servidor PostgreSQL de teste.
[assembly: CollectionBehavior(DisableTestParallelization = true)]


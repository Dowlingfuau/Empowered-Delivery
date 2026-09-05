namespace OperationalIntelligenceHub.Services
{
    public class SignalValidationService
    {
        private readonly SignalMappingLoaderService _mappingLoader;
        private readonly SignalRegistryService _registry;

        public SignalValidationService(
            SignalMappingLoaderService mappingLoader,
            SignalRegistryService registry)
        {
            _mappingLoader = mappingLoader;
            _registry = registry;
        }

        public async Task ValidateAsync()
        {
            var mappings = await _mappingLoader.GetMappings("maturity");

            var mappingSignals = mappings
                .Select(m => m.SignalName)
                .Distinct()
                .ToList();

            var missing = mappingSignals
                .Where(name => _registry.Get(name) == null)
                .ToList();

            if (missing.Any())
            {
                Console.WriteLine("🚨 SIGNAL REGISTRY VALIDATION FAILED");
                foreach (var m in missing)
                {
                    Console.WriteLine($"Missing definition: {m}");
                }

                throw new Exception("Signal registry is missing definitions");
            }

            Console.WriteLine("✅ Signal registry validation passed");
        }
    }
}
using System;
using System.Collections.Generic;

namespace OperationalIntelligenceHub.Services
{
    public enum DeepDiveType
    {
        Team,
        Backlog,
        Maturity,
        // add future tools here
    }

    public class DeepDiveService
    {
        private readonly List<DeepDiveType> _activeDeepDives = new();

        public IReadOnlyList<DeepDiveType> ActiveDeepDives => _activeDeepDives.AsReadOnly();

        public bool IsActive(DeepDiveType type) => _activeDeepDives.Contains(type);

        public void Toggle(DeepDiveType type)
        {
            if (_activeDeepDives.Contains(type))
                _activeDeepDives.Remove(type);
            else
                _activeDeepDives.Add(type); // append to maintain open order
        }
    }
}
using OperationalIntelligenceHub.Models;
using OperationalIntelligenceHub.Services;
using System.Collections.Generic;
using System.Net.Http;

public class TestRuleRepositoryService : RuleRepositoryService
{
    public TestRuleRepositoryService()
        : base(new HttpClient())
        {
            LoadDefaultRules();
        } 

    private void LoadDefaultRules()
    {
        AddDefaultBehaviourRules();
        AddDefaultFlowRules();
        AddDefaultSystemRules();
    }

    public void AddFlowRule(DerivedSignalRule rule)
    {
        FlowRules.Add(rule);
    }

    public void AddBehaviourRule(DerivedSignalRule rule)
    {
        BehaviourRules.Add(rule);
    }

    public void AddSystemRule(DerivedSignalRule rule)
    {
        SystemRules.Add(rule);
    }

    private void AddDefaultBehaviourRules()
    {
        AddBehaviourRule(new DerivedSignalRule
        {
            Name = "WeakProductOwnership",
            Domain = "Capability",
            Priority = 8,
            Confidence_Weight = 0.85,
            Rules = new List<RuleCondition>
            {
                new RuleCondition
                {
                    Condition = "ProductOwnershipScore < 0.4",
                    Result = "High"
                }
            },
            Interpretation = "Product ownership is weak"
        });
    }

    private void AddDefaultFlowRules()
    {
        AddFlowRule(new DerivedSignalRule
        {
            Name = "BacklogVolatility",
            Domain = "Flow",
            Priority = 6,
            Confidence_Weight = 0.75,
            Rules = new List<RuleCondition>
            {
                new RuleCondition
                {
                    Condition = "BacklogVolatility > 0.7",
                    Result = "High"
                }
            },
            Interpretation = "Backlog is unstable"
        });
    }

    private void AddDefaultSystemRules()
    {
        AddSystemRule(new DerivedSignalRule
        {
            Name = "DeliveryFactoryRisk",
            Domain = "System",
            Priority = 10,
            Confidence_Weight = 0.9,
            Rules = new List<RuleCondition>
            {
                new RuleCondition
                {
                    Condition = "WeakProductOwnership = High AND BacklogVolatility = High",
                    Result = "High"
                }
            },
            Interpretation = "Delivery is operating as a factory"
        });
    }
    public void OverrideBehaviourRule(DerivedSignalRule rule)
    {
        BehaviourRules.RemoveAll(r => r.Name == rule.Name);
        BehaviourRules.Add(rule);
    }
    public void OverrideFlowRule(DerivedSignalRule rule)
    {
        FlowRules.RemoveAll(r => r.Name == rule.Name);
        FlowRules.Add(rule);
    }
    public void OverrideSustemRule(DerivedSignalRule rule)
    {
        SystemRules.RemoveAll(r => r.Name == rule.Name);
        SystemRules.Add(rule);
    }
}
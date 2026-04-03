using Pholus.Editor.Analysis.Models;
using Pholus.Editor.Prompts.Attributes;
using Pholus.Editor.Prompts.Interfaces;

namespace Pholus.Editor.Prompts.BuiltIn
{
    [PromptProvider(Priority = 0)]
    public class SendMessageRule : IDetectionRule
    {
        public string IssueType => "sendmessage_usage";
        public string Title => "SendMessage/BroadcastMessage usage";
        public IssueSeverity DefaultSeverity => IssueSeverity.High;
        public int Priority => 0;

        public string DetectionDescription =>
            "- SendMessage(), BroadcastMessage() - uses reflection, very slow";

        public string FixInstructions => @"Fix by using direct method calls or events:
- If you have a reference to the target, call the method directly
- If you need loose coupling, use C# events or UnityEvents
- If you must use string-based messaging, at least cache the target
- Replace SendMessage(""MethodName"") with directReference.MethodName()
- Do not modify any other code";
    }
}

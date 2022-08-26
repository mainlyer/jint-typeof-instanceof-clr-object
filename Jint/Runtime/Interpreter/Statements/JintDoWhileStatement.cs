using Esprima.Ast;
using Jint.Native;
using Jint.Runtime.Interpreter.Expressions;

namespace Jint.Runtime.Interpreter.Statements
{
    /// <summary>
    /// http://www.ecma-international.org/ecma-262/5.1/#sec-12.6.1
    /// </summary>
    internal sealed class JintDoWhileStatement : JintStatement<DoWhileStatement>
    {
        private ProbablyBlockStatement _body;
        private string? _labelSetName;
        private JintExpression _test = null!;

        public JintDoWhileStatement(DoWhileStatement statement) : base(statement)
        {
        }

        protected override void Initialize(EvaluationContext context)
        {
            _body = new ProbablyBlockStatement(_statement.Body);
            _test = JintExpression.Build(context.Engine, _statement.Test);
            _labelSetName = _statement.LabelSet?.Name;
        }

        protected override Completion ExecuteInternal(EvaluationContext context)
        {
            JsValue v = Undefined.Instance;
            bool iterating;

            do
            {
                var completion = _body.Execute(context);
                if (!ReferenceEquals(completion.Value, null))
                {
                    v = completion.Value;
                }

                if (completion.Type != CompletionType.Continue || completion.Target != _labelSetName)
                {
                    if (completion.Type == CompletionType.Break && (completion.Target == null || completion.Target == _labelSetName))
                    {
                        return new Completion(CompletionType.Normal, v, null, Location);
                    }

                    if (completion.Type != CompletionType.Normal)
                    {
                        return completion;
                    }
                }

                if (context.DebugMode)
                {
                    context.Engine.DebugHandler.OnStep(_test._expression);
                }

                iterating = TypeConverter.ToBoolean(_test.GetValue(context).Value);
            } while (iterating);

            return NormalCompletion(v);
        }
    }
}

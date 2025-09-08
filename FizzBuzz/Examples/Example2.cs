public class Example2
{
    public static void Run(string[] args)
    {
        var ruleChain = FizzBuzzRuleFactory.CreateRuleChain();
        var publisher = new FizzBuzzPublisher<string>();
        var output = new ConsoleOutputStrategy();
        var runner = new FizzBuzzRunner(ruleChain, publisher, output);
        runner.Run(1, 100);
    }
}

public interface IOutputStrategy<T>
{
    void Output(T value);
}

public class ConsoleOutputStrategy : IOutputStrategy<string>
{
    public void Output(string value)
    {
        Console.WriteLine(value);
    }
}

public interface IFizzBuzzRule
{
    string Apply(int number, Func<int, string> next);
}

public class FizzRule : IFizzBuzzRule
{
    public string Apply(int number, Func<int, string> next)
    {
        if (number % 3 == 0)
            return "Fizz" + next(number);
        return next(number);
    }
}

public class BuzzRule : IFizzBuzzRule
{
    public string Apply(int number, Func<int, string> next)
    {
        if (number % 5 == 0)
            return "Buzz" + next(number);
        return next(number);
    }
}

public class NumberRule : IFizzBuzzRule
{
    public string Apply(int number, Func<int, string> next)
    {
        var result = next(number);
        return string.IsNullOrEmpty(result) ? number.ToString() : result;
    }
}

public static class FizzBuzzRuleFactory
{
    public static IFizzBuzzRule CreateRuleChain()
    {
        return new FizzRuleDecorator(
            new BuzzRuleDecorator(
                new NumberRuleDecorator(null)
            )
        );
    }
}

public abstract class RuleDecoratorBase : IFizzBuzzRule
{
    protected readonly IFizzBuzzRule _next;

    protected RuleDecoratorBase(IFizzBuzzRule next)
    {
        _next = next;
    }

    public abstract string Apply(int number, Func<int, string> next);
}

public class FizzRuleDecorator : RuleDecoratorBase
{
    public FizzRuleDecorator(IFizzBuzzRule next) : base(next)
    {
    }

    public override string Apply(int number, Func<int, string> next)
    {
        return new FizzRule().Apply(number, n => _next?.Apply(n, next) ?? "");
    }
}

public class BuzzRuleDecorator : RuleDecoratorBase
{
    public BuzzRuleDecorator(IFizzBuzzRule next) : base(next)
    {
    }

    public override string Apply(int number, Func<int, string> next)
    {
        return new BuzzRule().Apply(number, n => _next?.Apply(n, next) ?? "");
    }
}

public class NumberRuleDecorator : RuleDecoratorBase
{
    public NumberRuleDecorator(IFizzBuzzRule next) : base(next)
    {
    }

    public override string Apply(int number, Func<int, string> next)
    {
        return new NumberRule().Apply(number, n => _next?.Apply(n, next) ?? "");
    }
}

public class FizzBuzzPublisher<T>
{
    public event Action<T> OnOutput;

    public void Publish(T value)
    {
        OnOutput?.Invoke(value);
    }
}

public class FizzBuzzRunner
{
    private readonly IFizzBuzzRule _ruleChain;
    private readonly FizzBuzzPublisher<string> _publisher;
    private readonly IOutputStrategy<string> _outputStrategy;

    public FizzBuzzRunner(IFizzBuzzRule ruleChain, FizzBuzzPublisher<string> publisher,
        IOutputStrategy<string> outputStrategy)
    {
        _ruleChain = ruleChain;
        _publisher = publisher;
        _outputStrategy = outputStrategy;
        _publisher.OnOutput += _outputStrategy.Output;
    }

    public void Run(int start, int end)
    {
        for (int i = start; i <= end; i++)
        {
            var result = _ruleChain.Apply(i, _ => "");
            _publisher.Publish(result);
        }
    }
}
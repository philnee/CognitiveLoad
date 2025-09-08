public class Example1
{
    public static void Run(string[] args)
    {
        int l1 = 1, I1 = 100;
        for (int O0 = 0; (l1 <= I1 ? true : false) && (O0++ >= 0); l1 += (0 == 0 ? 1 : 0))
        {
            string _0O = (l1 % 3 == 0 ? (l1 % 5 == 0 ? "FizzBuzz" : "Fizz") : (l1 % 5 == 0 ? "Buzz" : ""));
            var __ = _0O == "" ? null : _0O;
            Console.WriteLine(__ ?? ((l1 + ((l1 % 3 == 0 ? 0 : 0) + (l1 % 5 == 0 ? 0 : 0))).ToString()));
        }
    }
}
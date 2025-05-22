// Program.cs
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("解析対象のディレクトリパスを引数として指定してください。");
            Console.WriteLine("例: dotnet run -- \"C:\\path\\to\\your\\project\"");
            return;
        }

        string targetDirectory = args[0];
        if (!Directory.Exists(targetDirectory))
        {
            Console.WriteLine($"指定されたディレクトリが見つかりません: {targetDirectory}");
            return;
        }

        long totalExecutableStatements = 0;
        var csFiles = Directory.EnumerateFiles(targetDirectory, "*.cs", SearchOption.AllDirectories);

        foreach (var filePath in csFiles)
        {
            try
            {
                string code = await File.ReadAllTextAsync(filePath);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                var root = await tree.GetRootAsync();

                var counter = new ExecutableStatementCounter();
                counter.Visit(root);
                // Console.WriteLine($"{filePath}: {counter.Count} executable statements"); // ファイルごとの表示
                totalExecutableStatements += counter.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ファイル処理中にエラー発生 ({filePath}): {ex.Message}");
            }
        }

        Console.WriteLine($"----------------------------------------------------");
        Console.WriteLine($"合計実行可能ステートメント数: {totalExecutableStatements}");
        Console.WriteLine($"----------------------------------------------------");
    }
}

public class ExecutableStatementCounter : CSharpSyntaxWalker
{
    public int Count { get; private set; }

    // usingディレクティブはカウントしないので、VisitUsingDirective はオーバーライドしない (または空にする)

    // 実行可能と見なすステートメントのVisitメソッドをオーバーライドしてカウント
    // (注意: このリストは完全ではありません。プロジェクトの特性に合わせて調整が必要です)

    public override void VisitExpressionStatement(ExpressionStatementSyntax node)
    {
        // 例: Console.WriteLine("Hello"); や someVariable = 10;
        CountStatement(node);
        base.VisitExpressionStatement(node);
    }

    public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
    {
        // 例: int x = 10; string s = GetString();
        // 初期化子がある場合のみ実行可能と見なすか、宣言自体をカウントするかは定義による
        // ここでは、初期化子を持つ宣言（実質的に代入が伴う）をカウントする例
        if (node.Declaration.Variables.Any(v => v.Initializer != null))
        {
            CountStatement(node);
        }
        base.VisitLocalDeclarationStatement(node);
    }

    public override void VisitReturnStatement(ReturnStatementSyntax node)
    {
        CountStatement(node);
        base.VisitReturnStatement(node);
    }

    public override void VisitIfStatement(IfStatementSyntax node)
    {
        // if (...) 自体も実行の一部と考える
        CountStatement(node);
        base.VisitIfStatement(node);
    }

    public override void VisitForEachStatement(ForEachStatementSyntax node)
    {
        CountStatement(node);
        base.VisitForEachStatement(node);
    }

    public override void VisitForStatement(ForStatementSyntax node)
    {
        CountStatement(node);
        base.VisitForStatement(node);
    }

    public override void VisitWhileStatement(WhileStatementSyntax node)
    {
        CountStatement(node);
        base.VisitWhileStatement(node);
    }

    public override void VisitDoStatement(DoStatementSyntax node)
    {
        CountStatement(node);
        base.VisitDoStatement(node);
    }

    public override void VisitSwitchStatement(SwitchStatementSyntax node)
    {
        CountStatement(node);
        base.VisitSwitchStatement(node);
    }

    public override void VisitThrowStatement(ThrowStatementSyntax node)
    {
        CountStatement(node);
        base.VisitThrowStatement(node);
    }

    public override void VisitTryStatement(TryStatementSyntax node)
    {
        CountStatement(node);
        base.VisitTryStatement(node);
    }

    public override void VisitUsingStatement(UsingStatementSyntax node)
    {
        // using (var disposable = new MyDisposable()) { ... }
        // これは実行可能なステートメント (リソース管理)
        // ユーザーが除外したいのは usingディレクティブ (例: using System;) と解釈
        CountStatement(node);
        base.VisitUsingStatement(node);
    }

    // 他にもカウントしたいステートメントがあれば追加 (例: LockStatement, YieldStatementなど)

    // --- 実行可能行としてカウントしないもの (明示的に除外するわけではないが、Visit対象に含めない) ---
    // UsingDirectiveSyntax (using System; など)
    // NamespaceDeclarationSyntax, TypeDeclarationSyntax (class, struct, interface, enum)
    // MethodDeclarationSyntax, ConstructorDeclarationSyntax, PropertyDeclarationSyntaxなどのシグネチャ部分
    // BlockSyntax ( { ... } 自体はカウントしない)
    // EmptyStatementSyntax ( ; のみの行など)

    private void CountStatement(SyntaxNode node)
    {
        // 1ステートメントを1行としてカウントする場合
        Count++;

        // もし物理的な「行」を数えたい場合、より複雑なロジックが必要になります。
        // (例: ステートメントが含まれる行の開始行番号と終了行番号を取得し、重複を除いてカウント)
        // var lineSpan = node.SyntaxTree.GetLineSpan(node.Span);
        // int lines = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;
        // ここでは簡略化のためステートメント数をカウントします。
    }
}

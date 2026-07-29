param(
    [Parameter(Mandatory)]
    [string] $AssemblyPath
)

$ErrorActionPreference = 'Stop'

if ($PSVersionTable.PSVersion -lt [version] '7.4') {
    throw "PowerShell 7.4 or later is required; found $($PSVersionTable.PSVersion)."
}

Add-Type -Path (Resolve-Path $AssemblyPath)

$context = [Pidamg.Expressions.EvaluationContext]::new()
$context.Set('quantity', 3)
$context.Set('unitPrice', 12)

$expression = [Pidamg.Expressions.ExpressionParser]::Parse('quantity * unitPrice')
$result = $expression.Evaluate($context)

if ($result -ne 36) {
    throw "Expected expression result 36; found $result."
}

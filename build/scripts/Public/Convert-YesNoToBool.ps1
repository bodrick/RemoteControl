function Convert-YesNoToBool
{
    param(
        [Parameter(Mandatory)]
        [ValidateSet('yes', 'no')]
        $value
    )

    $value -eq 'yes'
}

export default function handler(req, res) {
  res.setHeader('Content-Type', 'text/plain');
  res.status(200).send(`function Show-Menu($title, $options, $line) {
    $selected = 0
    $startTop = $line

    # Write the fixed title
    [System.Console]::SetCursorPosition(0, $startTop)
    [System.Console]::ForegroundColor = "Yellow"
    [System.Console]::WriteLine($title)
    [System.Console]::ForegroundColor = "White"

    while ($true) {
        # Draw options in fixed positions
        for ($i = 0; $i -lt $options.Count; $i++) {
            [System.Console]::SetCursorPosition(0, $startTop + $i + 1)
            if ($i -eq $selected) {
                # Highlighted option with > symbol
                [System.Console]::Write("> " + $options[$i] + "   ")
            } else {
                [System.Console]::Write("  " + $options[$i] + "   ")
            }
            # Clear rest of the line
            [System.Console]::Write(" " * ([System.Console]::WindowWidth - [System.Console]::CursorLeft))
        }

        $key = [System.Console]::ReadKey($true)
        switch ($key.Key) {
            "UpArrow"   { if ($selected -gt 0) { $selected-- } }
            "DownArrow" { if ($selected -lt $options.Count - 1) { $selected++ } }
            "Enter"     {
                # Show choice below the menu
                [System.Console]::SetCursorPosition(0, $startTop + $options.Count + 2)
                [System.Console]::WriteLine("You chose: " + $options[$selected] + "         ")
                return $options[$selected]
            }
        }
    }
}

# Maintain black background and clear screen
[System.Console]::BackgroundColor = "Black"
[System.Console]::ForegroundColor = "White"
[System.Console]::Clear()
[System.Console]::CursorVisible = $false

# Questions
$res1 = Show-Menu "First option:" @("Red","Green","Blue") 0
$res2 = Show-Menu "Second option:" @("Dog","Cat","Bird") 5
$res3 = Show-Menu "Third option:" @("Car","Motorbike","Bicycle") 10

# Show final results in place of menus
[System.Console]::Clear()
Write-Host "Final results:" -ForegroundColor Green
Write-Host "1) $res1"
Write-Host "2) $res2"
Write-Host "3) $res3"

# Pause
Write-Host "`nPress any key to exit..."
[System.Console]::ReadKey($true) | Out-Null
[System.Console]::CursorVisible = $true
`);
}

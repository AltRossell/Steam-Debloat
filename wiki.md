# Steam Debloat - Frequently Asked Questions (FAQ)

## Table of Contents

- [General Questions](#general-questions)
- [Installation and Usage](#installation-and-usage)
- [Operating Modes](#operating-modes)
- [Troubleshooting](#troubleshooting)
- [Security](#security)
- [Contributing](#contributing)

## General Questions

### What is Steam Debloat?

Steam Debloat is a tool designed to optimize Steam installations by removing unnecessary components and improving overall performance.

### What does the script do exactly?

The script performs several optimizations:

- Downloads and applies optimized configurations
- Enables downgrading to more stable Steam versions
- Configures Steam for optimal performance
- Removes unnecessary components

### Is Steam Debloat safe to use?

Yes, Steam Debloat is safe for the following reasons:

- It's open-source and can be reviewed by anyone
- Doesn't modify critical system files
- Only interacts with Steam-related files
- Includes error handling and detailed logging

## Installation and Usage

### What are the system requirements?

- Windows 10 or higher
- Administrator privileges
- PowerShell 5.1 or higher
- Internet connection

### How do I install Steam Debloat?

1. Download the batch from the repository or run the powershell command
2. Running the batch or PowerShell as an administrator
3. Execute the script following the instructions

### Should I make a backup before using the tool?

While the script is safe, it would be a very specific case therefore extreme or impossible for the script to damage any important steam files:

- Saved game files
- The "C:\Program Files (x86)\Steam\steamapps\common" folder which is where the data of our downloaded games is, if you want to have the games and not download them again make the backup of this specific folder

## Operating Modes

### Which mode should I choose?

**Normal Mode**

- Recommended for most users
- Includes all standard optimizations
- Balances performance and functionality

**Lite Mode**

- Basic optimizations
- Maintains more default features
- Ideal for users wanting minimal changes

**TEST Mode** **No support**

- Includes experimental features
- For advanced users
- May require more manual configuration

**TEST-Lite Mode** **No support**

- Experimental version of Lite mode
- Testing new features
- Safer than full TEST mode

**TEST-Version Mode** _Coming soon_ **No support**

- Allows specifying custom Steam version
- For users needing a specific version
- Requires URL of desired version

### Can I switch between modes?

Yes, you can run the script again and select a different mode. We recommend restarting Steam between changes.

## Troubleshooting

### Steam won't start after using the script

1. Verify all Steam processes are closed
2. Run the Steam.bat file from desktop
3. If persists, run Steam repair
4. Check logs at %TEMP%\Steam-Debloat.log
5. Open an Issues with your log.txt and I will fix it quickly

### Permission error when executing

1. Ensure PowerShell is run as administrator
2. Check for restrictive execution policies
3. Use command: `Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process`

### Connection issues

1. Verify your internet connection
2. Script will automatically retry downloads
3. Check if GitHub is accessible
4. Verify no firewall blocks

### Common Error Messages

- "Access Denied": Run as administrator
- "File Not Found": Verify Steam installation path
- "Network Error": Check internet connection
- "Process Already Running": Close Steam completely

## Security

### What information does the script collect?

- Only logs actions to local log file
- Doesn't send data to external servers
- Doesn't collect personal information

### Why does it need administrator privileges?

- To stop Steam processes
- To modify files in Program Files
- To apply system configurations

### Is my game data safe?

- Yes, the script doesn't modify game files
- Game saves remain untouched
- Steam library stays intact

## Contributing

### How can I contribute?

- Report bugs in Issues section
- Suggest improvements
- Submit pull requests
- Improve documentation
- Share the project

### How do I report an issue?

1. Open a new Issue on GitHub
2. Include logs (%TEMP%\Steam-Debloat.log)
3. Describe steps to reproduce
4. Mention your OS and version
5. Attach screenshots if relevant

### How do I suggest new features?

1. Verify feature doesn't exist
2. Open new Issue with "enhancement" label
3. Describe feature in detail
4. Explain why it would be useful
5. Include examples if possible

## Advanced Usage

### Log File Location

- Default: %TEMP%\Steam-Debloat.log
- Contains detailed operation history
- Useful for troubleshooting
- Automatically rotated to prevent size issues

### Recovery options

If something goes wrong:

1. Download the uninstall steam legacy batch from the repository
2. This batch updates steam to the latest current update and deletes settings affected by the script
3. Run Uninstall.bat
4. Contact support via GitHub Issues

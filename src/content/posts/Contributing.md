---
title: Contributing to Steam-Debloat
published: 2025-08-28
description: 'Complete guide for contributing to Steam-Debloat project - learn how to collaborate, report bugs, and help improve the gaming experience for everyone.'
image: ''
tags: ['contributing', 'development', 'collaboration', 'open-source']
category: 'Documentation'
draft: false 
lang: ''
---

<div style="text-align: center; margin: 2rem 0;">
  <img src="https://raw.githubusercontent.com/AltRossell/Steam-Debloat/main/assets/logo.png" alt="Steam Debloat Logo" width="200"/>
</div>

<div style="text-align: center; margin: 2rem 0;">
  We greatly appreciate your interest in contributing to Steam-Debloat! This guide will help you get started with contributing to our project and ensure a smooth collaboration process.
</div>

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
  - [Setting Up Your Development Environment](#setting-up-your-development-environment)
  - [Finding an Issue to Work On](#finding-an-issue-to-work-on)
- [Making Changes](#making-changes)
  - [Branching Strategy](#branching-strategy)
  - [Commit Messages](#commit-messages)
- [Submitting Changes](#submitting-changes)
  - [Pull Request Process](#pull-request-process)
  - [Code Review](#code-review)
- [Reporting Bugs](#reporting-bugs)
- [Suggesting Enhancements](#suggesting-enhancements)
- [Style Guidelines](#style-guidelines)
- [Additional Resources](#additional-resources)
- [License](#license)

## Code of Conduct

By participating in this project, you are expected to uphold our Code of Conduct. Please read it before contributing.

## Getting Started

### Setting Up Your Development Environment

1. Fork the repository on GitHub
2. Clone your fork locally
   ```bash
   git clone https://github.com/AltRossell/Steam-Debloat.git
   ```
3. Add the original repository as a remote named "upstream"
   ```bash
   git remote add upstream https://github.com/AltRossell/Steam-Debloat.git
   ```
4. Create a new branch for your feature or bug fix
   ```bash
   git checkout -b feature/your-feature-name
   ```

### Finding an Issue to Work On

- Check our [Issues](https://github.com/AltRossell/Steam-Debloat/issues) page for open tasks
- Look for issues tagged with "good first issue" or "help wanted"
- If you have a new idea, please open an issue to discuss it before starting work

## Making Changes

### Branching Strategy

- Use feature branches for all new changes
- Base your changes on the `main` branch
- Keep your branch up to date by regularly syncing with `upstream/main`

### Commit Messages

- Use the present tense ("Add feature" not "Added feature")
- Use the imperative mood ("Move cursor to..." not "Moves cursor to...")
- Limit the first line to 72 characters or less
- Reference issues and pull requests liberally after the first line

## Submitting Changes

### Pull Request Process

1. Ensure your code adheres to the project's style guidelines
2. Update the README.md with details of changes, if applicable
3. Increase the version numbers in any examples files and the README.md to the new version that this Pull Request would represent
4. Submit a pull request to the `main` branch

### Code Review

- All submissions, including those by project members, require review
- We use GitHub pull requests for this purpose
- Expect feedback and be prepared to make adjustments to your code

## Reporting Bugs

We use GitHub issues to track bugs. Report a bug by [opening a new issue](https://github.com/AltRossell/Steam-Debloat/issues/new); it's that easy!

When filing an issue, please include:

- A clear and descriptive title
- A detailed description of the issue
- Steps to reproduce the behavior
- Expected behavior
- Screenshots (if applicable)
- Your operating system and Steam-Debloat version

## Suggesting Enhancements

We welcome suggestions for enhancements. Please create an issue and include:

- A clear and descriptive title
- A detailed description of the proposed enhancement
- Any potential drawbacks or considerations
- If possible, a mock-up or sketch of the enhancement

## Style Guidelines

- Use clear and meaningful variable and function names
- Comment your code where necessary, especially for complex logic
- Follow the existing code structure and organization

### For PowerShell scripts:
- Use PascalCase for function names
- Use camelCase for variable names
- Use proper indentation (4 spaces)

### For Markdown files:
- Use ATX-style headers (# Header 1)
- Use fenced code blocks with language specification

## Additional Resources

- [GitHub Flow Guide](https://guides.github.com/introduction/flow/)
- [How to Contribute to Open Source](https://opensource.guide/how-to-contribute/)
- [About Pull Requests](https://help.github.com/articles/about-pull-requests/)

## License

By contributing to Steam-Debloat, you agree that your contributions will be licensed under the MIT License.

---

<div style="text-align: center; margin: 2rem 0;">
  Thank you for contributing to Steam-Debloat! Your efforts help make Steam better for everyone. 🎮✨
</div>
let selectedMode = null;
let currentLang = 'en';
let translations = {};

const languageConfig = {
  en: { flag: '🇺🇸', name: 'English', file: 'en.json' },
  es: { flag: '🇪🇸', name: 'Español', file: 'es.json' },
  pt: { flag: '🇧🇷', name: 'Português', file: 'pt.json' },
  fr: { flag: '🇫🇷', name: 'Français', file: 'fr.json' },
  de: { flag: '🇩🇪', name: 'Deutsch', file: 'de.json' },
  zh: { flag: '🇨🇳', name: '中文', file: 'zh.json' },
  jp: { flag: '🇯🇵', name: '日本語', file: 'jp.json' },
  kr: { flag: '🇰🇷', name: '한국어', file: 'kr.json' },
  nl: { flag: '🇳🇱', name: 'Nederlands', file: 'nl.json' },
  pl: { flag: '🇵🇱', name: 'Polski', file: 'pl.json' },
  it: { flag: '🇮🇹', name: 'Italiano', file: 'it.json' },
  in: { flag: '🇮🇳', name: 'हिंदी', file: 'in.json' },
  ru: { flag: '🇷🇺', name: 'Русский', file: 'ru.json' },
  sa: { flag: '🇸🇦', name: 'العربية', file: 'sa.json' },
  tr: { flag: '🇹🇷', name: 'Türkçe', file: 'tr.json' }
};

function detectBrowserLanguage() {
  const browserLang = navigator.language.slice(0, 2).toLowerCase();
  return languageConfig[browserLang] ? browserLang : 'en';
}

function initializeLanguage() {
  const urlParams = new URLSearchParams(window.location.search);
  const urlLang = urlParams.get('lang');
  const savedLang = localStorage.getItem('preferred-language');
  const browserLang = detectBrowserLanguage();
  
  let initialLang = 'en';
  if (urlLang && languageConfig[urlLang]) {
    initialLang = urlLang;
  } else if (savedLang && languageConfig[savedLang]) {
    initialLang = savedLang;
  } else {
    initialLang = browserLang;
  }
  
  return initialLang;
}

const embeddedTranslations = {
  en: {
    meta: {
      title: "Steam Debloat - Optimize your Steam experience",
      description: "Optimize your Steam experience with specific client versions for better performance and compatibility.",
      ogDescription: "Use specific versions of the Steam client for best performance and compatibility."
    },
    nav: {
      version: "v8.27",
      home: "Home",
      downloads: "Downloads",
      features: "Features",
      docs: "Documentation",
      faq: "FAQ",
      github: "GitHub"
    },
    hero: {
      badge: "New version v8.27 available",
      title: "Optimize your",
      titleGradient: "Steam experience",
      description: 'Use specific versions of the Steam client for <span class="text-steam-accent font-medium">better performance</span>, <span class="text-steam-accent font-medium">enhanced compatibility</span>, and an <span class="text-steam-accent font-medium">optimized gaming experience</span>.',
      downloadNow: "Download Now",
      viewCode: "View Code",
      stats: {
        downloads: "Downloads",
        rating: "Rating",
        openSource: "Open Source"
      }
    },
    downloads: {
      title: "Choose your",
      titleGradient: "version",
      description: "Select the configuration that best suits your needs and system requirements",
      versions: {
        steam2025: {
          title: "Steam 2025",
          description: "Latest version with all modern features and performance improvements.",
          features: ["Latest version", "Maximum performance", "New features"]
        },
        steam2022: {
          title: "Steam 2022",
          description: "December 2022 stable release, perfect for maximum compatibility.",
          features: ["Very stable", "Wide compatibility", "Extensively tested"]
        },
        steamLite: {
          title: "Steam Lite 2022",
          description: "Ultra-lightweight version with essentials for maximum speed and minimum resource usage.",
          features: ["Ultra lightweight", "Instant loading", "Minimal consumption"]
        },
        steamDual: {
          title: "Steam Dual",
          description: "Install both versions (2022 and 2025) to switch between them as needed.",
          features: ["Maximum flexibility", "Experimental", "For experts"]
        }
      },
      selectVersion: "Select a version",
      downloadSelected: "Download {version}",
      versionInfo: "Version v8.27 • Requires administrator permissions",
      security: {
        safe: "100% Safe",
        verified: "Verified Code",
        noMalware: "No Malware"
      }
    },
    features: {
      title: "Why",
      titleGradient: "Steam Debloat",
      description: "Discover all the benefits of optimizing your Steam client",
      list: [
        {
          emoji: "⚙️",
          title: "Automatic Optimization",
          description: "Smart settings that automatically optimize Steam for your specific system configuration."
        },
        {
          emoji: "⚡",
          title: "Express Installation",
          description: "Automated process that detects your system and applies the best settings instantly."
        },
        {
          emoji: "🛡️",
          title: "100% Secure",
          description: "Open source code verified by the community. No malicious modifications or security risks."
        },
        {
          emoji: "📈",
          title: "Performance Boost",
          description: "Up to 250% faster loading times and 40% less memory usage for a smooth experience."
        },
        {
          emoji: "🔄",
          title: "Fully Reversible",
          description: "All changes are completely reversible. Return to original version whenever you want."
        },
        {
          emoji: "🎮",
          title: "Gaming Optimized",
          description: "Specific gaming settings that reduce latency and improve overall gaming experience."
        }
      ],
      performance: {
        title: "Performance Comparison",
        original: "Original Steam",
        optimized: "Steam Debloat",
        improvement: "Improvement",
        loadingTime: "Loading Time",
        memoryUsage: "Memory Usage",
        speedBoost: "Speed Boost",
        memorySaved: "Memory Saved"
      }
    },
    docs: {
      title: "Documentation",
      description: "Everything you need to know to get started",
      systemRequirements: {
        title: "System Requirements",
        list: [
          "Windows 10/11 (64-bit)",
          "PowerShell 5.0 or higher",
          "Administrator permissions",
          "Internet connection"
        ]
      },
      installation: {
        title: "Installation Guide",
        steps: [
          {
            title: "Download the script",
            description: "Select your preferred version in the downloads section above"
          },
          {
            title: "Run as administrator",
            description: 'Right-click → "Run as administrator" on the downloaded file'
          },
          {
            title: "Automatic process",
            description: "The script automatically detects and optimizes your Steam installation"
          },
          {
            title: "Ready to use!",
            description: "Enjoy your optimized and enhanced Steam experience"
          }
        ]
      },
      codePreview: {
        title: "Code Preview"
      }
    },
    faq: {
      title: "Frequently Asked",
      titleGradient: "Questions",
      description: "We answer your most common questions about Steam Debloat",
      items: [
        {
          question: "Is Steam Debloat safe to use?",
          answer: "Absolutely. Steam Debloat is an open-source project that you can review on GitHub. It doesn't maliciously modify system files—it only applies official Steam settings to optimize performance. All changes are completely reversible."
        },
        {
          question: "What are the differences between versions?",
          answer: "Each version is optimized for different needs:",
          versions: [
            "Steam 2025: Latest version with all modern features and optimizations",
            "Steam 2022: Stable and tested version for maximum compatibility",
            "Steam Lite: Ultra-lightweight version for systems with limited resources",
            "Steam Dual: Allows you to switch between versions as needed"
          ]
        },
        {
          question: "Can I revert to the original Steam version?",
          answer: "Yes, all changes are completely reversible. The script automatically creates backups, and you can restore the original version by running the script again and selecting the restore option."
        },
        {
          question: "Does it affect my games or save data?",
          answer: "No, Steam Debloat only modifies the Steam client itself. It doesn't affect your games, save data, achievements, or any content in your library. All your games will continue to work exactly the same, but with improved performance."
        },
        {
          question: "Do I need technical knowledge to use it?",
          answer: "Not at all. Steam Debloat is designed to be used by anyone. Simply download the file, run it as administrator, and follow the on-screen instructions. The entire process is completely automated."
        }
      ]
    },
    footer: {
      title: "Steam Debloat",
      subtitle: "Open source project",
      links: {
        github: "GitHub",
        issues: "Issues",
        releases: "Releases",
        documentation: "Documentation"
      },
      copyright: "© 2024-2025 Steam Debloat • Open source project • Not affiliated with Valve Corporation",
      madeWith: "Made with ❤️ for the gaming community"
    },
    ui: {
      downloading: "Downloading...",
      selectFirst: "Select a version first"
    }
  }
};

async function loadTranslations(lang) {
  if (embeddedTranslations[lang]) {
    return embeddedTranslations[lang];
  }

  try {
    const response = await fetch(`/lang/${lang}.json`);
    if (response.ok) {
      return await response.json();
    }
  } catch (error) {
    console.warn(`Could not load external translations for ${lang}:`, error);
  }

  return embeddedTranslations.en;
}

function updateContent(translations) {
  document.querySelectorAll('[data-i18n]').forEach(element => {
    const key = element.getAttribute('data-i18n');
    const value = getNestedValue(translations, key);
    if (value) {
      element.textContent = value;
    }
  });

  document.querySelectorAll('[data-i18n-html]').forEach(element => {
    const key = element.getAttribute('data-i18n-html');
    const value = getNestedValue(translations, key);
    if (value) {
      element.innerHTML = value;
    }
  });

  updateFeatures(translations.features.list);
  updateVersionFeatures(translations.downloads.versions);
  updateSystemRequirements(translations.docs.systemRequirements.list);
  updateInstallationSteps(translations.docs.installation.steps);
  updateFAQ(translations.faq.items);

  if (selectedMode) {
    updateDownloadButton(selectedMode, translations);
  }
}

function getNestedValue(obj, key) {
  return key.split('.').reduce((o, k) => o && o[k], obj);
}

// Función mejorada para inicializar los dropdowns de idiomas
function initializeLanguageDropdowns() {
  const desktopDropdown = document.getElementById('language-dropdown');
  const mobileOptions = document.getElementById('mobile-language-options');
 
  if (!desktopDropdown || !mobileOptions) return;
  
  desktopDropdown.innerHTML = '';
  mobileOptions.innerHTML = '';
  
  Object.entries(languageConfig).forEach(([lang, config]) => {
    // Desktop dropdown con grid layout
    const desktopOption = document.createElement('div');
    desktopOption.className = 'language-option';
    desktopOption.setAttribute('data-lang', lang);
    desktopOption.innerHTML = `
      <span class="language-flag">${config.flag}</span>
      <span class="language-name">${config.name}</span>
    `;
    desktopOption.addEventListener('click', () => switchLanguage(lang));
    desktopDropdown.appendChild(desktopOption);
    
    // Mobile options mejorado
    const mobileOption = document.createElement('button');
    mobileOption.className = `language-option-mobile w-full text-left px-4 py-3 rounded-lg transition-all duration-200 flex items-center gap-3 ${
      lang === currentLang ? 'bg-steam-accent/20 text-steam-accent border border-steam-accent/30' : 'hover:bg-white/5 border border-transparent'
    }`;
    mobileOption.setAttribute('data-lang', lang);
    mobileOption.innerHTML = `
      <span class="language-flag text-lg">${config.flag}</span>
      <span class="language-name font-medium">${config.name}</span>
      ${lang === currentLang ? '<svg class="w-4 h-4 ml-auto text-steam-accent" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/></svg>' : ''}
    `;
    mobileOption.addEventListener('click', () => {
      switchLanguage(lang);
      closeMobileMenu();
    });
    mobileOptions.appendChild(mobileOption);
  });
}

// Función mejorada para cambiar idioma con feedback visual
async function switchLanguage(lang) {
  if (lang === currentLang) return;

  // Agregar clase de transición
  document.body.classList.add('language-switching');
  
  try {
    const newTranslations = await loadTranslations(lang);
    if (newTranslations) {
      currentLang = lang;
      translations = newTranslations;
      
      // Actualizar contenido
      updateContent(translations);
      updateLanguageButton();
      updateMobileLanguageOptions();
      
      localStorage.setItem('preferred-language', lang);
      document.documentElement.lang = lang;
      
      const url = new URL(window.location);
      url.searchParams.set('lang', lang);
      window.history.replaceState({}, '', url);
      
      // Cerrar dropdown
      const dropdown = document.getElementById('language-dropdown');
      if (dropdown) dropdown.classList.remove('active');
      
      console.log(`Language switched to: ${languageConfig[lang].name}`);
    }
  } catch (error) {
    console.error('Error switching language:', error);
  } finally {
    // Remover clase de transición
    setTimeout(() => {
      document.body.classList.remove('language-switching');
    }, 300);
  }
}

// Función para actualizar las opciones móviles
function updateMobileLanguageOptions() {
  const mobileOptions = document.getElementById('mobile-language-options');
  if (!mobileOptions) return;
  
  const options = mobileOptions.querySelectorAll('.language-option-mobile');
  options.forEach(option => {
    const lang = option.getAttribute('data-lang');
    
    if (lang) {
      const isActive = lang === currentLang;
      option.className = `language-option-mobile w-full text-left px-4 py-3 rounded-lg transition-all duration-200 flex items-center gap-3 ${
        isActive ? 'bg-steam-accent/20 text-steam-accent border border-steam-accent/30' : 'hover:bg-white/5 border border-transparent'
      }`;
      
      // Actualizar el checkmark
      const existingCheck = option.querySelector('svg');
      if (existingCheck && !isActive) {
        existingCheck.remove();
      } else if (!existingCheck && isActive) {
        option.innerHTML += '<svg class="w-4 h-4 ml-auto text-steam-accent" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/></svg>';
      }
    }
  });
}

// Función mejorada para actualizar el botón de idioma
function updateLanguageButton() {
  const config = languageConfig[currentLang];
  const button = document.getElementById('language-btn');
  
  if (!button || !config) return;
  
  const flag = button.querySelector('.language-flag');
  const code = button.querySelector('span:last-of-type');
  
  if (flag && code) {
    flag.textContent = config.flag;
    code.textContent = currentLang.toUpperCase();
  }
  
  // Actualizar opciones del dropdown
  document.querySelectorAll('.language-option').forEach(option => {
    const lang = option.getAttribute('data-lang');
    option.classList.toggle('active', lang === currentLang);
  });
}

// Función para manejar el scroll del dropdown en dispositivos pequeños
function handleDropdownScroll() {
  const dropdown = document.getElementById('language-dropdown');
  if (!dropdown) return;
  
  const activeOption = dropdown.querySelector('.language-option.active');
  if (activeOption && dropdown.classList.contains('active')) {
    setTimeout(() => {
      activeOption.scrollIntoView({
        behavior: 'smooth',
        block: 'nearest'
      });
    }, 100);
  }
}

function updateDownloadButton(mode, translations) {
  const downloadBtn = document.getElementById('downloadBtn');
  const downloadText = document.getElementById('downloadText');
  
  if (!downloadBtn || !downloadText) return;
  
  const versionName = getModeDisplayName(mode, translations);
  downloadText.textContent = translations.downloads.downloadSelected.replace('{version}', versionName);
}

function selectMode(element, mode) {
  document.querySelectorAll('.mode-card').forEach(card => {
    card.classList.remove('selected');
  });

  element.classList.add('selected');
  selectedMode = mode;

  const downloadBtn = document.getElementById('downloadBtn');
  const downloadText = document.getElementById('downloadText');

  if (!downloadBtn || !downloadText) return;

  downloadBtn.disabled = false;
  downloadBtn.classList.remove('glass-apple');
  downloadBtn.classList.add('btn-apple');

  const versionName = getModeDisplayName(mode, translations);
  downloadText.textContent = translations.downloads.downloadSelected.replace('{version}', versionName);
}

function getModeDisplayName(mode, translations = null) {
  if (!translations) translations = window.translations || embeddedTranslations.en;
  
  const names = {
    'Normal2025July': translations.downloads?.versions?.steam2025?.title || 'Steam 2025',
    'Normal2022dec': translations.downloads?.versions?.steam2022?.title || 'Steam 2022',
    'Lite2022dec': translations.downloads?.versions?.steamLite?.title || 'Steam Lite',
    'NormalBoth2022-2025': translations.downloads?.versions?.steamDual?.title || 'Steam Dual'
  };
  return names[mode] || mode;
}

function addLoadingState() {
  const btn = document.getElementById('downloadBtn');
  if (!btn) return;
  
  const originalText = btn.textContent;
  btn.disabled = true;
  btn.innerHTML = `
    <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-white inline" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
    </svg>
    ${translations.ui?.downloading || 'Downloading...'}
  `;

  setTimeout(() => {
    btn.disabled = false;
    btn.textContent = originalText;
  }, 2000);
}

function downloadSelected() {
  if (!selectedMode) return;

  addLoadingState();

  setTimeout(() => {
    const downloadUrls = {
      'Normal2025July': 'https://github.com/AltRossell/Steam-Debloat/releases/download/v8.27/Installer2025.bat',
      'Normal2022dec': 'https://github.com/AltRossell/Steam-Debloat/releases/download/v8.27/Installer2022dec.bat',
      'Lite2022dec': 'https://github.com/AltRossell/Steam-Debloat/releases/download/v8.27/InstallerLite2022dec.bat',
      'NormalBoth2022-2025': 'https://github.com/AltRossell/Steam-Debloat/releases/download/v8.27/Installer2022_2025.bat'
    };

    const downloadUrl = downloadUrls[selectedMode];
    if (!downloadUrl) return;

    const link = document.createElement('a');
    link.href = downloadUrl;
    link.download = downloadUrl.split('/').pop();
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }, 1000);
}

function updateFeatures(features) {
  const grid = document.getElementById('features-grid');
  if (!grid) return;
  
  grid.innerHTML = '';

  features.forEach(feature => {
    const featureCard = document.createElement('div');
    featureCard.className = 'glass-apple card-apple p-10 rounded-3xl text-center fade-on-scroll';
    featureCard.innerHTML = `
      <div class="w-16 h-16 mx-auto mb-8 rounded-2xl bg-gradient-to-br from-green-400 to-steam-accent flex items-center justify-center text-3xl">
        ${feature.emoji}
      </div>
      <h3 class="text-xl font-bold mb-4">${feature.title}</h3>
      <p class="text-white/60 leading-relaxed font-light">${feature.description}</p>
    `;
    grid.appendChild(featureCard);
  });
}

function updateVersionFeatures(versions) {
  const featureContainers = document.querySelectorAll('[data-version-features]');
  
  featureContainers.forEach(container => {
    const versionKey = container.getAttribute('data-version-features');
    const versionData = versions[versionKey];
    
    if (versionData && versionData.features) {
      const features = container.querySelectorAll('.text-white\\/70');
      features.forEach((feature, index) => {
        if (versionData.features[index]) {
          feature.textContent = versionData.features[index];
        }
      });
    }
  });
}

function updateSystemRequirements(requirements) {
  const container = document.getElementById('system-requirements');
  if (!container) return;
  
  container.innerHTML = '';

  requirements.forEach(requirement => {
    const reqElement = document.createElement('div');
    reqElement.className = 'flex items-center gap-4 p-4 rounded-2xl bg-white/5';
    reqElement.innerHTML = `
      <div class="w-8 h-8 rounded-full bg-green-400/20 flex items-center justify-center">
        <svg class="w-4 h-4 text-green-400" fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/>
        </svg>
      </div>
      <span class="text-white/90 font-medium">${requirement}</span>
    `;
    container.appendChild(reqElement);
  });
}

function updateInstallationSteps(steps) {
  const container = document.getElementById('installation-steps');
  if (!container) return;
  
  container.innerHTML = '';

  steps.forEach((step, index) => {
    const stepElement = document.createElement('div');
    stepElement.className = 'flex items-start gap-6';
    
    const isLast = index === steps.length - 1;
    const bgClass = isLast ? 'from-green-400 to-steam-accent' : 'from-steam-accent to-apple-blue';
    const icon = isLast ? '✓' : (index + 1).toString();
    
    stepElement.innerHTML = `
      <div class="w-10 h-10 rounded-full bg-gradient-to-br ${bgClass} flex items-center justify-center text-sm font-bold flex-shrink-0">${icon}</div>
      <div>
        <p class="font-semibold text-white/90 mb-2">${step.title}</p>
        <p class="text-sm text-white/50 font-light">${step.description}</p>
      </div>
    `;
    container.appendChild(stepElement);
  });
}

function updateFAQ(faqItems) {
  const container = document.getElementById('faq-container');
  if (!container) return;
  
  container.innerHTML = '';

  faqItems.forEach((item) => {
    const faqElement = document.createElement('div');
    faqElement.className = 'faq-item';
    
    let answerContent = item.answer;
    if (item.versions) {
      answerContent += '<div class="space-y-4 text-white/60 font-light mt-6">';
      item.versions.forEach(version => {
        const parts = version.split(': ');
        const title = parts[0];
        const description = parts[1] || '';
        const colorClass = getVersionColorClass(title);
        answerContent += `<div><strong class="${colorClass} font-medium">${title}:</strong> ${description}</div>`;
      });
      answerContent += '</div>';
    }
    
    faqElement.innerHTML = `
      <button class="w-full px-10 py-8 text-left flex items-center justify-between hover:bg-white/5 transition-colors" onclick="toggleFAQ(this)">
        <span class="font-semibold text-lg">${item.question}</span>
        <svg class="w-6 h-6 transform transition-transform duration-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
        </svg>
      </button>
      <div class="hidden px-10 pb-8">
        <div class="text-white/60 leading-relaxed font-light">${answerContent}</div>
      </div>
    `;
    container.appendChild(faqElement);
  });
}

function getVersionColorClass(title) {
  if (title.includes('2025')) return 'text-steam-accent';
  if (title.includes('2022')) return 'text-purple-400';
  if (title.includes('Lite')) return 'text-yellow-400';
  if (title.includes('Dual')) return 'text-pink-400';
  return 'text-steam-accent';
}

function openMobileMenu() {
  const menu = document.getElementById('mobile-menu');
  if (menu) {
    menu.classList.add('active');
    document.body.style.overflow = 'hidden';
  }
}

function closeMobileMenu() {
  const menu = document.getElementById('mobile-menu');
  if (menu) {
    menu.classList.remove('active');
    document.body.style.overflow = '';
  }
}

function scrollToSection(sectionId) {
  const section = document.getElementById(sectionId);
  if (section) {
    section.scrollIntoView({
      behavior: 'smooth',
      block: 'start'
    });
  }
}

function toggleFAQ(button) {
  const content = button.nextElementSibling;
  const icon = button.querySelector('svg');
  
  if (!content || !icon) return;

  if (content.classList.contains('hidden')) {
    content.classList.remove('hidden');
    content.style.maxHeight = content.scrollHeight + 'px';
    icon.style.transform = 'rotate(180deg)';
  } else {
    content.classList.add('hidden');
    content.style.maxHeight = '0';
    icon.style.transform = 'rotate(0deg)';
  }
}

function animateCounter(element) {
  const target = parseInt(element.getAttribute('data-target'));
  const duration = 2500;
  const increment = target / (duration / 16);
  let current = 0;

  const timer = setInterval(() => {
    current += increment;
    if (current >= target) {
      current = target;
      clearInterval(timer);
    }
    element.textContent = Math.floor(current).toLocaleString() + '+';
  }, 16);
}

function setupEventListeners() {
  const languageBtn = document.getElementById('language-btn');
  const languageDropdown = document.getElementById('language-dropdown');
  
  if (languageBtn && languageDropdown) {
    languageBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      languageDropdown.classList.toggle('active');
      const svg = languageBtn.querySelector('svg');
      if (svg) {
        svg.style.transform = languageDropdown.classList.contains('active') ? 'rotate(180deg)' : 'rotate(0deg)';
      }
    });

    document.addEventListener('click', (e) => {
      if (!languageBtn.contains(e.target) && !languageDropdown.contains(e.target)) {
        languageDropdown.classList.remove('active');
        const svg = languageBtn.querySelector('svg');
        if (svg) svg.style.transform = 'rotate(0deg)';
      }
    });
  }

  const mobileMenuBtn = document.getElementById('mobile-menu-btn');
  const mobileMenuClose = document.getElementById('mobile-menu-close');
  
  if (mobileMenuBtn) {
    mobileMenuBtn.addEventListener('click', openMobileMenu);
  }
  if (mobileMenuClose) {
    mobileMenuClose.addEventListener('click', closeMobileMenu);
  }

  const links = document.querySelectorAll('a[href^="#"]');
  links.forEach(link => {
    link.addEventListener('click', (e) => {
      e.preventDefault();
      const target = document.querySelector(link.getAttribute('href'));
      if (target) {
        target.scrollIntoView({
          behavior: 'smooth',
          block: 'start'
        });
        closeMobileMenu();
      }
    });
  });
}

function setupScrollAnimations() {
  const observerOptions = {
    threshold: 0.1,
    rootMargin: '0px 0px -100px 0px'
  };

  const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        entry.target.classList.add('visible');

        const counter = entry.target.querySelector('.counter');
        if (counter && !counter.classList.contains('animated')) {
          counter.classList.add('animated');
          animateCounter(counter);
        }
      }
    });
  }, observerOptions);

  document.querySelectorAll('.fade-on-scroll').forEach(el => {
    observer.observe(el);
  });
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', async () => {
  try {
    const initialLang = initializeLanguage();
    currentLang = initialLang;
    translations = await loadTranslations(currentLang);
    
    initializeLanguageDropdowns();
    updateContent(translations);
    updateLanguageButton();
    
    console.log(`Initialized with language: ${languageConfig[currentLang].name}`);
    
    setupEventListeners();
    setupScrollAnimations();
    
  } catch (error) {
    console.error('Error initializing application:', error);
    currentLang = 'en';
    translations = embeddedTranslations.en;
    updateContent(translations);
  }
});

// Export functions to global scope for HTML onclick handlers
window.scrollToSection = scrollToSection;
window.selectMode = selectMode;
window.downloadSelected = downloadSelected;
window.toggleFAQ = toggleFAQ;
window.switchLanguage = switchLanguage;

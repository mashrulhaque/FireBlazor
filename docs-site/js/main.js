// FireBlazor Documentation Site JavaScript

document.addEventListener('DOMContentLoaded', () => {
  initMobileMenu();
  initSidebarToggle();
  initCopyButtons();
  initSmoothScroll();
  initActiveLinks();
  initScrollSpy();
});

// Mobile Menu Toggle
function initMobileMenu() {
  const mobileMenuBtn = document.querySelector('.mobile-menu-btn');
  const nav = document.querySelector('.nav');

  if (mobileMenuBtn && nav) {
    mobileMenuBtn.addEventListener('click', () => {
      nav.classList.toggle('open');
      mobileMenuBtn.setAttribute(
        'aria-expanded',
        nav.classList.contains('open')
      );
    });
  }
}

// Sidebar Toggle for Mobile
function initSidebarToggle() {
  const sidebarToggle = document.querySelector('.sidebar-toggle');
  const sidebar = document.querySelector('.sidebar');
  const overlay = document.querySelector('.sidebar-overlay');

  if (sidebarToggle && sidebar) {
    sidebarToggle.addEventListener('click', () => {
      sidebar.classList.toggle('open');
      document.body.classList.toggle('sidebar-open');
    });
  }

  if (overlay) {
    overlay.addEventListener('click', () => {
      sidebar?.classList.remove('open');
      document.body.classList.remove('sidebar-open');
    });
  }

  // Close sidebar on escape key
  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && sidebar?.classList.contains('open')) {
      sidebar.classList.remove('open');
      document.body.classList.remove('sidebar-open');
    }
  });
}

// Copy Code Button
function initCopyButtons() {
  const codeBlocks = document.querySelectorAll('pre');

  codeBlocks.forEach((block) => {
    // Create copy button
    const copyBtn = document.createElement('button');
    copyBtn.className = 'copy-btn';
    copyBtn.innerHTML = `
      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
        <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
      </svg>
    `;
    copyBtn.setAttribute('aria-label', 'Copy code');
    copyBtn.setAttribute('title', 'Copy to clipboard');

    // Add click handler
    copyBtn.addEventListener('click', async () => {
      const code = block.querySelector('code')?.textContent || block.textContent;

      try {
        await navigator.clipboard.writeText(code);
        copyBtn.innerHTML = `
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="20 6 9 17 4 12"></polyline>
          </svg>
        `;
        copyBtn.classList.add('copied');

        setTimeout(() => {
          copyBtn.innerHTML = `
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
              <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
            </svg>
          `;
          copyBtn.classList.remove('copied');
        }, 2000);
      } catch (err) {
        console.error('Failed to copy:', err);
      }
    });

    // Position the button
    block.style.position = 'relative';
    block.appendChild(copyBtn);
  });
}

// Smooth Scroll for Anchor Links
function initSmoothScroll() {
  document.querySelectorAll('a[href^="#"]').forEach((anchor) => {
    anchor.addEventListener('click', (e) => {
      const targetId = anchor.getAttribute('href');
      if (targetId === '#') return;

      const target = document.querySelector(targetId);
      if (target) {
        e.preventDefault();
        const headerOffset = 80;
        const elementPosition = target.getBoundingClientRect().top;
        const offsetPosition = elementPosition + window.pageYOffset - headerOffset;

        window.scrollTo({
          top: offsetPosition,
          behavior: 'smooth'
        });

        // Update URL without scrolling
        history.pushState(null, '', targetId);
      }
    });
  });
}

// Highlight Active Nav Links
function initActiveLinks() {
  const currentPath = window.location.pathname;
  const navLinks = document.querySelectorAll('.nav-link, .dropdown-item, .sidebar-link');

  navLinks.forEach((link) => {
    const href = link.getAttribute('href');
    if (href) {
      const linkPath = href.split('/').pop();
      const currentPage = currentPath.split('/').pop() || 'index.html';

      if (linkPath === currentPage) {
        link.classList.add('active');
      }
    }
  });
}

// Scroll Spy for Sidebar
function initScrollSpy() {
  const sections = document.querySelectorAll('h2[id], h3[id]');
  const sidebarLinks = document.querySelectorAll('.sidebar-link');

  if (sections.length === 0 || sidebarLinks.length === 0) return;

  const observerOptions = {
    rootMargin: '-80px 0px -80% 0px',
    threshold: 0
  };

  const observer = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting) {
        const id = entry.target.getAttribute('id');

        sidebarLinks.forEach((link) => {
          link.classList.remove('active');
          if (link.getAttribute('href') === `#${id}`) {
            link.classList.add('active');
          }
        });
      }
    });
  }, observerOptions);

  sections.forEach((section) => observer.observe(section));
}

// Add syntax highlighting classes to code blocks
function highlightCode() {
  const codeBlocks = document.querySelectorAll('pre code');

  codeBlocks.forEach((block) => {
    let html = block.innerHTML;

    // C# keywords
    const keywords = [
      'var', 'await', 'async', 'new', 'return', 'if', 'else', 'foreach',
      'in', 'using', 'public', 'private', 'protected', 'class', 'interface',
      'void', 'string', 'int', 'bool', 'true', 'false', 'null', 'this'
    ];

    keywords.forEach((keyword) => {
      const regex = new RegExp(`\\b(${keyword})\\b`, 'g');
      html = html.replace(regex, '<span class="keyword">$1</span>');
    });

    // Strings
    html = html.replace(/"([^"\\]|\\.)*"/g, '<span class="string">$&</span>');

    // Comments
    html = html.replace(/\/\/.*$/gm, '<span class="comment">$&</span>');

    // Numbers
    html = html.replace(/\b(\d+)\b/g, '<span class="number">$1</span>');

    block.innerHTML = html;
  });
}

// Animate elements on scroll
function initScrollAnimations() {
  const animatedElements = document.querySelectorAll('.animate-on-scroll');

  const observer = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add('animate-fade-in');
          observer.unobserve(entry.target);
        }
      });
    },
    { threshold: 0.1 }
  );

  animatedElements.forEach((el) => observer.observe(el));
}

// Theme toggle (for future use)
function toggleTheme() {
  const root = document.documentElement;
  const currentTheme = root.getAttribute('data-theme');
  const newTheme = currentTheme === 'light' ? 'dark' : 'light';

  root.setAttribute('data-theme', newTheme);
  localStorage.setItem('theme', newTheme);
}

// Load saved theme
function loadTheme() {
  const savedTheme = localStorage.getItem('theme') || 'dark';
  document.documentElement.setAttribute('data-theme', savedTheme);
}

// Utility: Debounce function
function debounce(func, wait) {
  let timeout;
  return function executedFunction(...args) {
    const later = () => {
      clearTimeout(timeout);
      func(...args);
    };
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
  };
}

// Utility: Throttle function
function throttle(func, limit) {
  let inThrottle;
  return function executedFunction(...args) {
    if (!inThrottle) {
      func(...args);
      inThrottle = true;
      setTimeout(() => (inThrottle = false), limit);
    }
  };
}

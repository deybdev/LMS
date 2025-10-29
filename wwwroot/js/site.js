/**
 * G2 Academy LMS - Frontend JavaScript
 * Enhanced user experience with modern interactions
 */

// Enhanced State Management
class UIState {
    constructor() {
        this.loading = false;
        this.error = null;
        this.data = null;
    }
    
    setLoading(loading) {
        this.loading = loading;
        this.updateLoadingStates();
    }
    
    setError(error) {
        this.error = error;
        this.showErrorMessage(error);
    }
    
    updateLoadingStates() {
        const buttons = document.querySelectorAll('.btn-custom[data-loading]');
        buttons.forEach(btn => {
            if (this.loading) {
                btn.disabled = true;
                btn.innerHTML = '<span class="loading-spinner"></span> Loading...';
            } else {
                btn.disabled = false;
                btn.innerHTML = btn.getAttribute('data-original-text') || 'Submit';
            }
        });
    }
    
    showErrorMessage(message) {
        this.showToast('error', message);
    }
    
    showToast(type, message) {
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.innerHTML = `
            <div class="toast-content">
                <i class="fas fa-${type === 'error' ? 'exclamation-circle' : 'check-circle'}"></i>
                <span>${message}</span>
            </div>
        `;
        
        // Add toast styles if not already present
        if (!document.querySelector('#toast-styles')) {
            const style = document.createElement('style');
            style.id = 'toast-styles';
            style.textContent = `
                .toast {
                    position: fixed;
                    top: 20px;
                    right: 20px;
                    padding: var(--space-md) var(--space-lg);
                    border-radius: var(--radius-md);
                    background: white;
                    box-shadow: var(--shadow-lg);
                    z-index: 1000;
                    opacity: 0;
                    transform: translateX(100%);
                    transition: all 0.3s ease;
                }
                .toast.show { opacity: 1; transform: translateX(0); }
                .toast-error { border-left: 4px solid var(--danger-color); }
                .toast-success { border-left: 4px solid var(--success-color); }
                .toast-content { display: flex; align-items: center; gap: var(--space-sm); }
                .toast i { color: var(--danger-color); }
                .toast-success i { color: var(--success-color); }
            `;
            document.head.appendChild(style);
        }
        
        document.body.appendChild(toast);
        setTimeout(() => toast.classList.add('show'), 100);
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => document.body.removeChild(toast), 300);
        }, 3000);
    }
}

const uiState = new UIState();

// Enhanced smooth scrolling with offset for fixed headers
document.addEventListener('DOMContentLoaded', function() {
    // Smooth scrolling for anchor links
    const links = document.querySelectorAll('a[href^="#"]');
    
    links.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            
            const targetId = this.getAttribute('href').substring(1);
            const targetSection = document.getElementById(targetId);
            
            if (targetSection) {
                const headerOffset = 80; // Account for fixed header
                const elementPosition = targetSection.getBoundingClientRect().top;
                const offsetPosition = elementPosition + window.pageYOffset - headerOffset;
                
                window.scrollTo({
                    top: offsetPosition,
                    behavior: 'smooth'
                });
            }
        });
    });

    // Enhanced animation with staggered loading
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver((entries) => {
        entries.forEach((entry, index) => {
            if (entry.isIntersecting) {
                setTimeout(() => {
                    entry.target.style.opacity = '1';
                    entry.target.style.transform = 'translateY(0)';
                    entry.target.classList.add('animate-in');
                }, index * 100); // Stagger animation
                observer.unobserve(entry.target); // Only animate once
            }
        });
    }, observerOptions);

    // Observe all cards and sections
    const animatedElements = document.querySelectorAll('.card-custom, .assignment-card, .hero-section h1, .hero-section p');
    animatedElements.forEach((element, index) => {
        element.style.opacity = '0';
        element.style.transform = 'translateY(30px)';
        element.style.transition = `opacity 0.6s ease ${index * 0.1}s, transform 0.6s ease ${index * 0.1}s`;
        observer.observe(element);
    });
    
    // Add CSS animation class
    const style = document.createElement('style');
    style.textContent = `
        .animate-in {
            animation: fadeInUp 0.6s ease forwards;
        }
        @keyframes fadeInUp {
            from {
                opacity: 0;
                transform: translateY(30px);
            }
            to {
                opacity: 1;
                transform: translateY(0);
            }
        }
    `;
    document.head.appendChild(style);

    // Login Modal Functionality
    const loginModal = document.getElementById('loginModal');
    const loginForm = document.getElementById('loginForm');
    
    if (loginModal) {
        // Show custom backdrop when modal opens
        loginModal.addEventListener('show.bs.modal', function () {
            document.body.style.overflow = 'hidden';
        });

        // Hide custom backdrop when modal closes
        loginModal.addEventListener('hidden.bs.modal', function () {
            document.body.style.overflow = 'auto';
        });

        // Handle backdrop click to close modal
        loginModal.addEventListener('click', function(e) {
            if (e.target === this) {
                const modal = bootstrap.Modal.getInstance(this);
                if (modal) modal.hide();
            }
        });

        // Handle login form submission
        if (loginForm) {
            loginForm.addEventListener('submit', function(e) {
                e.preventDefault();
                
                const username = document.getElementById('username') || document.getElementById('email');
                const password = document.getElementById('password');
                
                // Enhanced form validation
                if (!validateLoginForm(username, password)) {
                    return;
                }
                
                // Show loading state
                const submitBtn = this.querySelector('button[type="submit"]');
                const originalText = submitBtn.textContent;
                uiState.setLoading(true);
                submitBtn.setAttribute('data-original-text', originalText);
                
                // Simulate login process with better error handling
                setTimeout(() => {
                    if (username.value.trim() && password.value.trim()) {
                        uiState.showToast('success', 'Login successful! Redirecting...');
                        setTimeout(() => {
                            window.location.href = '/Student/Dashboard';
                        }, 1000);
                    } else {
                        uiState.setLoading(false);
                        uiState.setError('Invalid credentials. Please check your username and password.');
                    }
                }, 1500);
            });
        }
        
        // Enhanced form validation
        function validateLoginForm(username, password) {
            let isValid = true;
            
            // Clear previous errors
            clearFieldErrors();
            
            if (!username || !username.value.trim()) {
                showFieldError(username, 'Username is required');
                isValid = false;
            }
            
            if (!password || !password.value.trim()) {
                showFieldError(password, 'Password is required');
                isValid = false;
            } else if (password.value.length < 3) {
                showFieldError(password, 'Password must be at least 3 characters');
                isValid = false;
            }
            
            return isValid;
        }
        
        function showFieldError(field, message) {
            field.classList.add('is-invalid');
            const errorDiv = document.createElement('div');
            errorDiv.className = 'invalid-feedback';
            errorDiv.textContent = message;
            field.parentNode.appendChild(errorDiv);
        }
        
        function clearFieldErrors() {
            const errorElements = document.querySelectorAll('.invalid-feedback');
            errorElements.forEach(el => el.remove());
            
            const invalidFields = document.querySelectorAll('.is-invalid');
            invalidFields.forEach(field => field.classList.remove('is-invalid'));
        }
    }
    
    // Mobile Navigation Toggle
    const mobileNavToggle = document.getElementById('mobile-nav-toggle');
    const mobileNav = document.getElementById('mobile-nav');
    
    if (mobileNavToggle && mobileNav) {
        mobileNavToggle.addEventListener('click', function() {
            const isVisible = mobileNav.style.display !== 'none';
            mobileNav.style.display = isVisible ? 'none' : 'block';
            
            // Update button icon
            const icon = this.querySelector('i');
            if (icon) {
                icon.className = isVisible ? 'fas fa-bars' : 'fas fa-times';
            }
            
            // Update aria-expanded
            this.setAttribute('aria-expanded', !isVisible);
        });
        }
    }
});
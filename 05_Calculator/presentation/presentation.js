/* ═══════════════════════════════════════════════════════════════════════════
   CT287 Presentation Deck - Horizontal Carousel Controller
   ═══════════════════════════════════════════════════════════════════════════ */

(function () {
    'use strict';

    let currentIndex = 0;
    const slides = document.querySelectorAll('.slide');
    const totalSlides = slides.length;
    const track = document.getElementById('slidesTrack');
    const prevBtn = document.getElementById('prevBtn');
    const nextBtn = document.getElementById('nextBtn');
    const counter = document.getElementById('slideCounter');
    const progressBar = document.getElementById('top-progress-bar');
    const dotsContainer = document.getElementById('paginationDots');
    const fullscreenBtn = document.getElementById('fullscreenBtn');

    // Create Pagination Dots
    function initDots() {
        dotsContainer.innerHTML = '';
        for (let i = 0; i < totalSlides; i++) {
            const dot = document.createElement('div');
            dot.className = 'dot' + (i === 0 ? ' active' : '');
            dot.title = `Slide ${i + 1}`;
            dot.addEventListener('click', () => goToSlide(i));
            dotsContainer.appendChild(dot);
        }
    }

    // Go to specific slide index
    function goToSlide(index) {
        if (index < 0) index = 0;
        if (index >= totalSlides) index = totalSlides - 1;
        currentIndex = index;

        // Slide horizontal track
        track.style.transform = `translateX(-${currentIndex * 100}vw)`;

        // Update active class on slides
        slides.forEach((slide, idx) => {
            if (idx === currentIndex) {
                slide.classList.add('active');
            } else {
                slide.classList.remove('active');
            }
        });

        // Update counter & progress bar
        const paddedCurrent = String(currentIndex + 1).padStart(2, '0');
        const paddedTotal = String(totalSlides).padStart(2, '0');
        counter.textContent = `${paddedCurrent} / ${paddedTotal}`;
        progressBar.style.width = `${((currentIndex + 1) / totalSlides) * 100}%`;

        // Update active dot
        const dots = dotsContainer.querySelectorAll('.dot');
        dots.forEach((dot, idx) => {
            if (idx === currentIndex) {
                dot.classList.add('active');
            } else {
                dot.classList.remove('active');
            }
        });

        // Update Prev / Next button states
        prevBtn.disabled = currentIndex === 0;
        nextBtn.disabled = currentIndex === totalSlides - 1;
    }

    function nextSlide() {
        if (currentIndex < totalSlides - 1) {
            goToSlide(currentIndex + 1);
        }
    }

    function prevSlide() {
        if (currentIndex > 0) {
            goToSlide(currentIndex - 1);
        }
    }

    function toggleFullscreen() {
        if (!document.fullscreenElement) {
            document.documentElement.requestFullscreen().catch(err => {
                console.warn(`Fullscreen error: ${err.message}`);
            });
            fullscreenBtn.textContent = '⛶ Thu nhỏ';
        } else {
            if (document.exitFullscreen) {
                document.exitFullscreen();
                fullscreenBtn.textContent = '⛶ Toàn màn hình';
            }
        }
    }

    // Keyboard navigation
    window.addEventListener('keydown', (e) => {
        // Prevent default spacebar scrolling
        if (e.code === 'Space' && e.target === document.body) {
            e.preventDefault();
        }

        switch (e.key) {
            case 'ArrowRight':
            case 'PageDown':
            case ' ':
            case 'Enter':
                nextSlide();
                break;
            case 'ArrowLeft':
            case 'PageUp':
            case 'Backspace':
                prevSlide();
                break;
            case 'Home':
                goToSlide(0);
                break;
            case 'End':
                goToSlide(totalSlides - 1);
                break;
            case 'f':
            case 'F':
                toggleFullscreen();
                break;
        }
    });

    // Button event listeners
    prevBtn.addEventListener('click', prevSlide);
    nextBtn.addEventListener('click', nextSlide);
    fullscreenBtn.addEventListener('click', toggleFullscreen);

    // Touch Swipe Support for Tablets / Mobile
    let touchStartX = 0;
    let touchEndX = 0;

    window.addEventListener('touchstart', (e) => {
        touchStartX = e.changedTouches[0].screenX;
    }, { passive: true });

    window.addEventListener('touchend', (e) => {
        touchEndX = e.changedTouches[0].screenX;
        handleSwipe();
    }, { passive: true });

    function handleSwipe() {
        const threshold = 50;
        if (touchEndX < touchStartX - threshold) {
            // Swiped left -> go to next
            nextSlide();
        } else if (touchEndX > touchStartX + threshold) {
            // Swiped right -> go to prev
            prevSlide();
        }
    }

    // Initialize
    initDots();
    goToSlide(0);

})();

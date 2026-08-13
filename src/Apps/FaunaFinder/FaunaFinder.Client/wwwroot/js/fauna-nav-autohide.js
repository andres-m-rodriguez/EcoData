// Gets the mobile chrome out of the way while the reader is heading down the
// page, and gives it back the moment they turn around. Both bars read the same
// attribute, so the top bar and the tab bar clear out together.
//
// The state lives in a data attribute on <html> rather than in a class on the
// bars themselves. Their classes are rendered by Blazor, so anything written
// there from JS is lost on the next re-render; an attribute on the root element
// is nobody else's business, and the stylesheets key off it.
//
// Nothing here knows the phone breakpoint. The component that starts this
// renders on small screens only, so the watcher exists exactly when the bars
// do, and the one definition of "phone" stays in the markup and the sheets.
//
// The document is the scrollport — the shell sets min-height rather than
// making an inner element scroll — so a plain window listener sees everything.

const ATTRIBUTE = 'data-nav-autohide';

// Distance in the current direction before the bar reacts. Hiding takes a
// deliberate push; showing takes noticeably less, because a reader who turns
// back wants the bar now, and "scrolling up a bit" should be enough.
const HIDE_AFTER_PX = 24;
const SHOW_AFTER_PX = 12;

// Near the top the bar is always out: there is nothing to read up there yet,
// and a page that opens with the bar hidden looks broken.
const TOP_ZONE_PX = 80;

let listening = false;
let hidden = false;
let lastY = 0;

// Distance travelled since the last direction change, not since the last
// event — a single flick fires many small deltas, and each on its own is
// below either threshold.
let travel = 0;

let frame = 0;

function currentY() {
    // Clamped: iOS reports negative values while rubber-banding past the top,
    // which would otherwise read as an upward scroll that never happened.
    return Math.max(0, window.scrollY || document.documentElement.scrollTop || 0);
}

function setHidden(next) {
    if (next === hidden) {
        return;
    }

    hidden = next;

    if (next) {
        document.documentElement.setAttribute(ATTRIBUTE, 'hidden');
    } else {
        document.documentElement.removeAttribute(ATTRIBUTE);
    }
}

function update() {
    frame = 0;

    const y = currentY();
    const delta = y - lastY;
    lastY = y;

    if (y <= TOP_ZONE_PX) {
        travel = 0;
        setHidden(false);
        return;
    }

    if (delta === 0) {
        return;
    }

    // A change of direction starts the count again, so the thresholds measure
    // one continuous movement rather than the net of a scroll back and forth.
    if ((delta > 0) !== (travel > 0)) {
        travel = 0;
    }

    travel += delta;

    if (travel >= HIDE_AFTER_PX) {
        setHidden(true);
        travel = 0;
    } else if (travel <= -SHOW_AFTER_PX) {
        setHidden(false);
        travel = 0;
    }
}

function onScroll() {
    // Coalesce to one read per frame: scroll fires far faster than paint, and
    // reading scrollY is a layout read.
    if (frame === 0) {
        frame = window.requestAnimationFrame(update);
    }
}

export function start() {
    if (listening) {
        return;
    }

    lastY = currentY();
    travel = 0;
    listening = true;

    window.addEventListener('scroll', onScroll, { passive: true });
}

export function stop() {
    if (!listening) {
        return;
    }

    window.removeEventListener('scroll', onScroll);
    listening = false;

    if (frame !== 0) {
        window.cancelAnimationFrame(frame);
        frame = 0;
    }

    // Leave the bar visible: the attribute outlives the component that set it.
    setHidden(false);
}

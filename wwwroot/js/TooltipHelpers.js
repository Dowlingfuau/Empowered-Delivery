window.uiHelpers = window.uiHelpers || {};

window.uiHelpers.getTooltipPositionForElement = (element, placement = 'right', offset = 8) => {
    try {
        if (!element) return null;
        const rect = element.getBoundingClientRect();
        const vw = window.innerWidth;
        const vh = window.innerHeight;
        const tooltipWidth = 260; // estimated

        let left, top;
        switch (placement) {
            case 'right':
                left = rect.right + offset;
                top = rect.top + rect.height / 2;
                if (left + tooltipWidth > vw - 8) {
                    left = rect.left - tooltipWidth - offset;
                }
                break;
            case 'left':
                left = rect.left - tooltipWidth - offset;
                top = rect.top + rect.height / 2;
                if (left < 8) left = rect.right + offset;
                break;
            case 'bottom':
                left = rect.left + rect.width / 2 - tooltipWidth / 2;
                top = rect.bottom + offset;
                break;
            default: // top
                left = rect.left + rect.width / 2 - tooltipWidth / 2;
                top = rect.top - offset;
        }

        top = Math.max(8, Math.min(vh - 8, top));
        left = Math.max(8, Math.min(vw - 8, left));

        return { top: Math.round(top), left: Math.round(left) };
    }
    catch (e) {
        return null;
    }
};

window.uiHelpers.getTooltipPositionForElementId = (id, placement = 'right', offset = 8) => {
    try {
        const el = document.getElementById(id);
        if (!el) return null;
        return window.uiHelpers.getTooltipPositionForElement(el, placement, offset);
    }
    catch (e) {
        return null;
    }
};

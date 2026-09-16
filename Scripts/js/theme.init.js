// Animate
(function ($) {

    'use strict';

    if ($.isFunction($.fn['appear'])) {

        $(function () {
            $('[data-plugin-animate], [data-appear-animation]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginAnimate)) {
                    $this.themePluginAnimate(opts);
                }
            });
        });

    }

})(jQuery);

// Carousel
(function ($) {

    'use strict';

    if ($.isFunction($.fn['owlCarousel'])) {

        $(function () {
            $('[data-plugin-carousel]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginCarousel)) {
                    $this.themePluginCarousel(opts);
                }
            });
        });

    }

})(jQuery);

// Chart Circular
(function ($) {

    'use strict';

    if ($.isFunction($.fn['easyPieChart'])) {

        $(function () {
            $('[data-plugin-chart-circular], .circular-bar-chart:not(.manual)').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginChartCircular)) {
                    $this.themePluginChartCircular(opts);
                }
            });
        });

    }

})(jQuery);

// Codemirror
(function ($) {

    'use strict';

    if (typeof CodeMirror !== 'undefined') {

        $(function () {
            $('[data-plugin-codemirror]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginCodeMirror)) {
                    $this.themePluginCodeMirror(opts);
                }
            });
        });

    }

})(jQuery);

// Colorpicker
(function ($) {

    'use strict';

    if ($.isFunction($.fn['colorpicker'])) {

        $(function () {
            $('[data-plugin-colorpicker]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginColorPicker)) {
                    $this.themePluginColorPicker(opts);
                }
            });
        });

    }

})(jQuery);

// Datepicker
(function ($) {

    'use strict';

    if ($.isFunction($.fn['bootstrapDP'])) {

        $(function () {
            $('[data-plugin-datepicker]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginDatePicker)) {
                    $this.themePluginDatePicker(opts);
                }
            });
        });

    }

})(jQuery);

// Header Menu Nav
(function (theme, $) {

    'use strict';

    if (typeof theme.Nav !== 'undefined' && $.isFunction(theme.Nav.initialize)) {
        theme.Nav.initialize();
    } else {
        console.error('theme.Nav is undefined or not initialized');
    }

})(window.theme, jQuery);

// iosSwitcher
(function ($) {

    'use strict';

    if (typeof Switch !== 'undefined' && $.isFunction(Switch)) {

        $(function () {
            $('[data-plugin-ios-switch]').each(function () {
                var $this = $(this);

                if ($.isFunction($this.themePluginIOS7Switch)) {
                    $this.themePluginIOS7Switch();
                }
            });
        });

    }

})(jQuery);

// Lightbox
(function ($) {

    'use strict';

    if ($.isFunction($.fn['magnificPopup'])) {

        $(function () {
            $('[data-plugin-lightbox], .lightbox:not(.manual)').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginLightbox)) {
                    $this.themePluginLightbox(opts);
                }
            });
        });

    }

})(jQuery);

// Portlets
(function ($) {

    'use strict';

    if (typeof NProgress !== 'undefined' && $.isFunction(NProgress.configure)) {

        NProgress.configure({
            showSpinner: false,
            ease: 'ease',
            speed: 750
        });

    }

})(jQuery);

// Markdown
(function ($) {

    'use strict';

    if ($.isFunction($.fn['markdown'])) {

        $(function () {
            $('[data-plugin-markdown-editor]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginMarkdownEditor)) {
                    $this.themePluginMarkdownEditor(opts);
                }
            });
        });

    }

})(jQuery);

// Masked Input
(function ($) {

    'use strict';

    if ($.isFunction($.fn['mask'])) {

        $(function () {
            $('[data-plugin-masked-input]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginMaskedInput)) {
                    $this.themePluginMaskedInput(opts);
                }
            });
        });

    }

})(jQuery);

// MaxLength
(function ($) {

    'use strict';

    if ($.isFunction($.fn['maxlength'])) {

        $(function () {
            $('[data-plugin-maxlength]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginMaxLength)) {
                    $this.themePluginMaxLength(opts);
                }
            });
        });

    }

})(jQuery);

// MultiSelect
(function ($) {

    'use strict';

    if ($.isFunction($.fn['multiselect'])) {

        $(function () {
            $('[data-plugin-multiselect]').each(function () {

                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginMultiSelect)) {
                    $this.themePluginMultiSelect(opts);
                }

            });
        });

    }

})(jQuery);

// Popover
(function ($) {

    'use strict';

    if ($.isFunction($.fn['popover'])) {
        $('[data-toggle=popover]').popover();
    }

})(jQuery);

// Scroll to Top
(function (theme, $) {
    'use strict';
    if (typeof theme.PluginScrollToTop !== 'undefined') {
        theme.PluginScrollToTop.initialize();
    }
})(window.theme, jQuery);

// Scrollable
(function ($) {

    'use strict';

    if ($.isFunction($.fn['nanoScroller'])) {

        $(function () {
            $('[data-plugin-scrollable]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginScrollable)) {
                    $this.themePluginScrollable(opts);
                }
            });
        });

    }

})(jQuery);

// Select2
(function ($) {

    'use strict';

    if ($.isFunction($.fn['select2'])) {

        $(function () {
            $('[data-plugin-selectTwo]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginSelect2)) {
                    $this.themePluginSelect2(opts);
                }
            });
        });

    }

})(jQuery);

// Finalize theme skeleton initialization
(function (theme, $) {

    'use strict';

    theme = theme || {};

    if ($.isFunction(theme.Skeleton.initialize)) {
        theme.Skeleton.initialize();
    }

})(window.theme, jQuery);

// Mailbox
(function ($) {

    'use strict';

    $(function () {
        $('[data-mailbox]').each(function () {
            var $this = $(this);

            if ($.isFunction($this.themeMailbox)) {
                $this.themeMailbox();
            }
        });
    });

})(jQuery);
// Animate
(function ($) {

    'use strict';

    if ($.isFunction($.fn['appear'])) {

        $(function () {
            $('[data-plugin-animate], [data-appear-animation]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginAnimate)) {
                    $this.themePluginAnimate(opts);
                }
            });
        });

    }

})(jQuery);

// Carousel
(function ($) {

    'use strict';

    if ($.isFunction($.fn['owlCarousel'])) {

        $(function () {
            $('[data-plugin-carousel]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginCarousel)) {
                    $this.themePluginCarousel(opts);
                }
            });
        });

    }

})(jQuery);

// Chart Circular
(function ($) {

    'use strict';

    if ($.isFunction($.fn['easyPieChart'])) {

        $(function () {
            $('[data-plugin-chart-circular], .circular-bar-chart:not(.manual)').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginChartCircular)) {
                    $this.themePluginChartCircular(opts);
                }
            });
        });

    }

})(jQuery);

// Codemirror
(function ($) {

    'use strict';

    if (typeof CodeMirror !== 'undefined') {

        $(function () {
            $('[data-plugin-codemirror]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginCodeMirror)) {
                    $this.themePluginCodeMirror(opts);
                }
            });
        });

    }

})(jQuery);

// Colorpicker
(function ($) {

    'use strict';

    if ($.isFunction($.fn['colorpicker'])) {

        $(function () {
            $('[data-plugin-colorpicker]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginColorPicker)) {
                    $this.themePluginColorPicker(opts);
                }
            });
        });

    }

})(jQuery);

// Datepicker
(function ($) {

    'use strict';

    if ($.isFunction($.fn['bootstrapDP'])) {

        $(function () {
            $('[data-plugin-datepicker]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginDatePicker)) {
                    $this.themePluginDatePicker(opts);
                }
            });
        });

    }

})(jQuery);

// Header Menu Nav
(function (theme, $) {

    'use strict';

    if (typeof theme.Nav !== 'undefined' && $.isFunction(theme.Nav.initialize)) {
        theme.Nav.initialize();
    } else {
        console.error('theme.Nav is undefined or not initialized');
    }

})(window.theme, jQuery);

// iosSwitcher
(function ($) {

    'use strict';

    if (typeof Switch !== 'undefined' && $.isFunction(Switch)) {

        $(function () {
            $('[data-plugin-ios-switch]').each(function () {
                var $this = $(this);

                if ($.isFunction($this.themePluginIOS7Switch)) {
                    $this.themePluginIOS7Switch();
                }
            });
        });

    }

})(jQuery);

// Lightbox
(function ($) {

    'use strict';

    if ($.isFunction($.fn['magnificPopup'])) {

        $(function () {
            $('[data-plugin-lightbox], .lightbox:not(.manual)').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginLightbox)) {
                    $this.themePluginLightbox(opts);
                }
            });
        });

    }

})(jQuery);

// Portlets
(function ($) {

    'use strict';

    if (typeof NProgress !== 'undefined' && $.isFunction(NProgress.configure)) {

        NProgress.configure({
            showSpinner: false,
            ease: 'ease',
            speed: 750
        });

    }

})(jQuery);

// Markdown
(function ($) {

    'use strict';

    if ($.isFunction($.fn['markdown'])) {

        $(function () {
            $('[data-plugin-markdown-editor]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginMarkdownEditor)) {
                    $this.themePluginMarkdownEditor(opts);
                }
            });
        });

    }

})(jQuery);

// Masked Input
(function ($) {

    'use strict';

    if ($.isFunction($.fn['mask'])) {

        $(function () {
            $('[data-plugin-masked-input]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginMaskedInput)) {
                    $this.themePluginMaskedInput(opts);
                }
            });
        });

    }

})(jQuery);

// MaxLength
(function ($) {

    'use strict';

    if ($.isFunction($.fn['maxlength'])) {

        $(function () {
            $('[data-plugin-maxlength]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginMaxLength)) {
                    $this.themePluginMaxLength(opts);
                }
            });
        });

    }

})(jQuery);

// MultiSelect
(function ($) {

    'use strict';

    if ($.isFunction($.fn['multiselect'])) {

        $(function () {
            $('[data-plugin-multiselect]').each(function () {

                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginMultiSelect)) {
                    $this.themePluginMultiSelect(opts);
                }

            });
        });

    }

})(jQuery);

// Popover
(function ($) {

    'use strict';

    if ($.isFunction($.fn['popover'])) {
        $('[data-toggle=popover]').popover();
    }

})(jQuery);

// Scroll to Top
(function (theme, $) {
    'use strict';
    if (typeof theme.PluginScrollToTop !== 'undefined') {
        theme.PluginScrollToTop.initialize();
    }
})(window.theme, jQuery);

// Scrollable
(function ($) {

    'use strict';

    if ($.isFunction($.fn['nanoScroller'])) {

        $(function () {
            $('[data-plugin-scrollable]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginScrollable)) {
                    $this.themePluginScrollable(opts);
                }
            });
        });

    }

})(jQuery);

// Select2
(function ($) {

    'use strict';

    if ($.isFunction($.fn['select2'])) {

        $(function () {
            $('[data-plugin-selectTwo]').each(function () {
                var $this = $(this),
                    opts = {};

                var pluginOptions = $this.data('plugin-options');
                if (pluginOptions) opts = pluginOptions;

                if ($.isFunction($this.themePluginSelect2)) {
                    $this.themePluginSelect2(opts);
                }
            });
        });

    }

})(jQuery);

// Finalize theme skeleton initialization
(function (theme, $) {

    'use strict';

    theme = theme || {};

    if ($.isFunction(theme.Skeleton.initialize)) {
        theme.Skeleton.initialize();
    }

})(window.theme, jQuery);

// Mailbox
(function ($) {

    'use strict';

    $(function () {
        $('[data-mailbox]').each(function () {
            var $this = $(this);

            if ($.isFunction($this.themeMailbox)) {
                $this.themeMailbox();
            }
        });
    });

})(jQuery);

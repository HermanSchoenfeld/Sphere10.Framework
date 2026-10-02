// PascalCase interop entry points shared with the Blazor components.
window.ShowModal = () => {
    $("#modal").modal({
        backdrop: "static",
        keyboard: false
    });
}

window.HideModal = () => {
    $("#modal").modal('hide')
}

window.AddDropdownHover = () => {
    $('.hover-dropdown').hover(function () {
            $('.hover-dropdown > .dropdown-menu').addClass('show');
        },
        function () {
            $(this).removeClass('show');
            $('.hover-dropdown > .dropdown-menu').removeClass('show');
        });
}

window.InitializeToolTips = () => {
    $('[data-toggle="tooltip"]').tooltip()
}

window.InitializeSearchDropdowns = () => {
    var input = $('.search-input');
    
    input.keyup(() => {
        if (input.val().length === 0)
        {
            $('.search-input-results').removeClass('show');
        }
        else 
        {
            $('.search-input-results').addClass('show');
        }
    });

    input.blur(() => {
        $('.search-input-results').removeClass('show');
    });
}

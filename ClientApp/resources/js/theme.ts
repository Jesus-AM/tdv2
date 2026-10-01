import { createTheme } from '@mui/material/styles';
export const theme = createTheme({
    palette: {
        primary: { main: '#245bb6', dark: '#183e80' },
        secondary: { main: '#157e83' },
        background: { default: '#f4f7fb', paper: '#fff' },
        text: { primary: '#20314d', secondary: '#65748a' },
        divider: '#e4eaf2',
        success: { main: '#23836c' },
        warning: { main: '#a76a16' },
    },
    typography: {
        fontFamily: 'Roboto, sans-serif',
        fontSize: 14,
        h1: { fontSize: '1.65rem', fontWeight: 700, letterSpacing: '-.025em' },
        h2: { fontSize: '1.05rem', fontWeight: 700 },
        h3: { fontSize: '.92rem', fontWeight: 600 },
        body2: { fontSize: '.8125rem', lineHeight: 1.6 },
        button: { textTransform: 'none', fontWeight: 600, letterSpacing: 0 },
    },
    shape: { borderRadius: 10 },
    components: {
        MuiButton: {
            defaultProps: { disableElevation: true, size: 'small' },
            styleOverrides: { root: { minHeight: 36, paddingInline: 14 } },
        },
        MuiIconButton: { defaultProps: { size: 'small' } },
        MuiTextField: {
            defaultProps: { size: 'small', variant: 'outlined' },
            styleOverrides: { root: { background: '#fff' } },
        },
        MuiFormControl: { defaultProps: { size: 'small' } },
        MuiPaper: { defaultProps: { elevation: 0 }, styleOverrides: { outlined: { borderColor: '#e0e7f0' } } },
        MuiTableCell: {
            styleOverrides: {
                head: { background: '#f6f8fc', fontSize: '.75rem', fontWeight: 600, color: '#5c6d85' },
                root: { borderColor: '#e8edf4', padding: '12px 14px' },
            },
        },
        MuiChip: {
            defaultProps: { size: 'small' },
            styleOverrides: { root: { fontSize: '.7rem', fontWeight: 500, borderRadius: 6 } },
        },
        MuiAlert: { styleOverrides: { root: { fontSize: '.8125rem', alignItems: 'center' } } },
        MuiTab: {
            styleOverrides: { root: { textTransform: 'none', minHeight: 46, fontSize: '.8125rem', fontWeight: 500 } },
        },
    },
});

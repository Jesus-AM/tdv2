// Mismos recursos y estilos del portal; Competencias mantiene UACJ en la cabecera.
export default function InstitutionalFooter({ variant = 'digital' }: { variant?: 'digital' | 'internal' }) {
    return <footer className={`institutional-footer ${variant === 'digital' ? 'cd-footer' : 'app-footer'}`}><div className={`${variant === 'digital' ? 'cd-wrap' : 'app-footer-inner'} institutional-signatures`}>
        {variant === 'internal' && <span className="institutional-logo"><img src="/images/competencias-digitales/firma-1.png" alt="UACJ Inspira el Futuro" width="1186" height="112" /></span>}
        <div className="institutional-signature institutional-directorate"><span>Dirección General de</span><strong>Tecnologías de la Información</strong></div>
        {['Desarrollo de Sistemas', 'Tecnología Educativa', 'Operación y Redes', 'Inteligencia de Datos'].map(name =>
            <div className="institutional-signature" key={name}><span>Subdirección de</span><strong>{name}</strong></div>)}
    </div></footer>;
}

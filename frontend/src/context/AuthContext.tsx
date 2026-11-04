import { createContext, useContext } from 'react';
export interface Usuario { id: string; cuenta: string; nombre: string; rol: 'Administrador' | 'Prevencion' | 'Jefatura'; establecimientoId?: string; activo: boolean }
export const AuthContext = createContext<Usuario | null>(null);
export const useAuth = () => { const user = useContext(AuthContext); if (!user) throw new Error('Sesión requerida'); return user; };

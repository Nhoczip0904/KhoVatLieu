import React, { useState } from 'react';
import { 
  LayoutDashboard, 
  Package, 
  Users, 
  FileText, 
  PlusCircle, 
  Settings,
  ChevronRight,
  Menu,
  X
} from 'lucide-react';
import Dashboard from './views/Dashboard';
import MasterCatalog from './views/MasterCatalog';
import Projects from './views/Projects';
import { useStorage, DATA_KEYS } from './hooks/useStorage';

const App = () => {
  const [activeView, setActiveView] = useState('dashboard');
  const [isSidebarOpen, setIsSidebarOpen] = useState(true);

  const navItems = [
    { id: 'dashboard', label: 'Bảng điều khiển', icon: LayoutDashboard },
    { id: 'catalog', label: 'Danh mục vật liệu', icon: Package },
    { id: 'projects', label: 'Dự án / Khách hàng', icon: Users },
  ];

  return (
    <div className="flex min-h-screen bg-[#0a0a0c]">
      {/* Sidebar */}
      <aside className={`
        ${isSidebarOpen ? 'w-64' : 'w-20'} 
        transition-all duration-300 border-r border-white/5 bg-[#141417] flex flex-col
      `}>
        <div className="p-6 flex items-center gap-3">
          <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center">
            <Package className="w-5 h-5 text-white" />
          </div>
          {isSidebarOpen && <span className="font-bold text-xl font-outfit">G-Warehouse</span>}
        </div>

        <nav className="flex-1 px-4 py-4">
          {navItems.map((item) => (
            <button
              key={item.id}
              onClick={() => setActiveView(item.id)}
              className={`
                w-full flex items-center gap-3 p-3 rounded-xl transition-all mb-2
                ${activeView === item.id 
                  ? 'bg-indigo-500/10 text-indigo-400 border border-indigo-500/20' 
                  : 'text-slate-400 hover:bg-white/5 hover:text-white'}
              `}
            >
              <item.icon className="w-5 h-5" />
              {isSidebarOpen && <span className="font-medium">{item.label}</span>}
            </button>
          ))}
        </nav>

        <div className="p-4 border-t border-white/5">
          <button className="w-full flex items-center gap-3 p-3 rounded-xl text-slate-400 hover:bg-white/5 hover:text-white transition-all">
            <Settings className="w-5 h-5" />
            {isSidebarOpen && <span className="font-medium">Cài đặt</span>}
          </button>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-y-auto">
        <header className="h-16 border-b border-white/5 bg-[#0a0a0c]/80 backdrop-blur-md flex items-center justify-between px-8 sticky top-0 z-10">
          <h2 className="text-xl font-semibold font-outfit">
            {navItems.find(i => i.id === activeView)?.label}
          </h2>
          <div className="flex items-center gap-4">
            <div className="text-right">
              <div className="text-sm font-medium">Quản trị viên</div>
              <div className="text-xs text-slate-500">Admin Mode</div>
            </div>
            <div className="w-10 h-10 rounded-full bg-indigo-500/20 border border-indigo-500/30 flex items-center justify-center text-indigo-400 font-bold">
              A
            </div>
          </div>
        </header>

        <div className="p-8">
          {activeView === 'dashboard' && <Dashboard />}
          {activeView === 'catalog' && <MasterCatalog />}
          {activeView === 'projects' && <Projects />}
        </div>
      </main>
    </div>
  );
};

export default App;
